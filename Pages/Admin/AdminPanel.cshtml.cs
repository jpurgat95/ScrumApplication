using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

[Authorize(Roles = "Admin")]
public class AdminPanelModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEventRepository _eventRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly IHubContext<UpdatesHub> _hubContext;

    public List<UserDto> Users { get; set; }

    public AdminPanelModel(UserManager<IdentityUser> userManager, IEventRepository eventRepo, 
        ITaskRepository taskRepo, IHubContext<UpdatesHub> hubContext)
    {
        _userManager = userManager;
        _eventRepo = eventRepo;
        _taskRepo = taskRepo;
        Users = new List<UserDto>();
        _hubContext = hubContext;
    }

    [BindProperty]
    public string NewPassword { get; set; }

    public async Task OnGetAsync()
    {
        var allUsers = _userManager.Users.ToList();
        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            Users.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Roles = roles
            });
        }
    }

    // Akcja zmiany hasła
    public async Task<IActionResult> OnPostChangeUserPasswordAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(NewPassword))
        {
            ModelState.AddModelError("", "Id użytkownika i nowe hasło są wymagane.");
            await OnGetAsync(); // do ponownego załadowania listy użytkowników
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            ModelState.AddModelError("", "Użytkownik nie znaleziony.");
            await OnGetAsync();
            return Page();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            await OnGetAsync();
            return Page();
        }

        TempData["SuccessMessage"] = $"Hasło użytkownika {user.UserName} zostało zmienione.";
        return RedirectToPage();
    }

    // Akcja usuwania użytkownika wraz z eventami i zadaniami
    public async Task<IActionResult> OnPostDeleteUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            var events = await _eventRepo.GetEventsAsync(user.Id, isAdmin: true);

            // Odfiltrowanie eventów, których właścicielem jest aktualny user
            var userEvents = events.Where(ev => ev.UserId == user.Id).ToList();

            foreach (var ev in userEvents)
                await _eventRepo.DeleteEventAsync(ev);

            var tasks = await _taskRepo.GetTasksAsync(user.Id, isAdmin: true);

            // Podobnie dla zadań, jeśli metoda GetTasksAsync zwraca więcej niż usera
            var userTasks = tasks.Where(task => task.UserId == user.Id).ToList();

            foreach (var task in userTasks)
                await _taskRepo.DeleteTaskAsync(task);

            await _userManager.DeleteAsync(user);
            TempData["SuccessMessage"] = $"Użytkownik {user.UserName} wraz z powiązanymi danymi został usunięty.";
            // Powiadomienie użytkownika o usunięciu konta
            await _hubContext.Clients.User(user.Id).SendAsync("ForceLogoutWithToast");
        }
        else
        {
            TempData["ErrorMessage"] = "Użytkownik nie istnieje.";
        }

        return RedirectToPage();
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public IList<string> Roles { get; set; }
    }
}
