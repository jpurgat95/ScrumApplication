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
        Users = new List<UserDto>();

        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                // Pomijamy użytkowników z rolą Admin
                continue;
            }

            Users.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Roles = roles
            });
        }
    }
    public async Task<IActionResult> OnPostForcePasswordResetAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            ModelState.AddModelError("", "Id użytkownika jest wymagane.");
            await OnGetAsync();
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            ModelState.AddModelError("", "Użytkownik nie znaleziony.");
            await OnGetAsync();
            return Page();
        }

        // Generuj token resetu hasła
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Przekonwertuj token na URL friendly (jesli chcesz przesłać w URL)
        var encodedToken = System.Net.WebUtility.UrlEncode(token);

        // Stwórz link do resetu hasła (dostosuj ścieżkę do swojej aplikacji)
        var resetUrl = Url.Page("/ResetPassword", null, new { userId = user.Id, token = encodedToken }, Request.Scheme);


        // Wyślij powiadomienie do użytkownika przez SignalR z linkiem do resetu
        await _hubContext.Clients.User(user.Id).SendAsync("ForcePasswordReset", resetUrl);

        TempData["SuccessMessage"] = $"Wymuszono reset hasła dla użytkownika {user.UserName}.";
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeleteUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Użytkownik nie istnieje.";
            return RedirectToPage();
        }

        // Pobierz eventy
        var events = await _eventRepo.GetEventsAsync(user.Id, isAdmin: true);
        var userEvents = events.Where(ev => ev.UserId == user.Id);
        // Usuń eventy jeden po drugim
        foreach (var ev in userEvents)
        {
            await _eventRepo.DeleteEventAsync(ev);
        }

        // Pobierz zadania
        var tasks = await _taskRepo.GetTasksAsync(user.Id, isAdmin: true);
        var userTasks = tasks.Where(t => t.UserId == user.Id);
        // Usuń zadania jeden po drugim
        foreach (var task in userTasks)
        {
            await _taskRepo.DeleteTaskAsync(task);
        }

        // Usuń użytkownika
        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania użytkownika.";
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Użytkownik {user.UserName} wraz z powiązanymi danymi został usunięty.";

        // Wyloguj użytkownika
        await _hubContext.Clients.User(user.Id).SendAsync("ForceLogoutWithToast");
        return RedirectToPage();
    }


    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public IList<string> Roles { get; set; }
    }
}
