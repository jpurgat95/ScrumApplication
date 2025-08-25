using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ScrumApplication.Pages.Tasks
{
    [Authorize] // Sprawdź, czy użytkownik jest zalogowany
    public class IndexModel : PageModel
    {
        private readonly ScrumDbContext _context;
        private readonly IHubContext<UpdatesHub> _hubContext;

        public IndexModel(ScrumDbContext context, IHubContext<UpdatesHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [BindProperty]
        [Required(ErrorMessage = "Tytuł jest wymagany")]
        public string? Title { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Opis jest wymagany")]
        public string? Description { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Data rozpoczęcia jest wymagana")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [BindProperty]
        [Required(ErrorMessage = "Data zakończenia jest wymagana")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddHours(1);
        // ID wybranego wydarzenia
        [BindProperty]
        [Required(ErrorMessage = "Wybór wydarzenia jest wymagany")]
        public int EventId { get; set; }

        public List<TaskItem> Tasks = new();

        // Lista wydarzeń do SelectList w formularzu
        public List<ScrumEvent> Events { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Admin"))
            {
                // Admin widzi wszystkie zadania
                Tasks = await _context.Tasks
                    .Include(t => t.ScrumEvent)
                    .Include(t => t.User)
                    .OrderBy(t => t.StartDate)
                    .ToListAsync();

                // Admin widzi tylko swoje wydarzenia
                Events = await _context.Events
                    .Where(e => e.UserId == userId)
                    .OrderBy(e => e.StartDate)
                    .ToListAsync();
            }
            else
            {
                // User widzi tylko swoje zadania
                Tasks = await _context.Tasks
                    .Include(t => t.ScrumEvent)
                    .Where(t => t.UserId == userId)
                    .OrderBy(t => t.StartDate)
                    .ToListAsync();

                // User widzi tylko swoje wydarzenia
                Events = await _context.Events
                    .Where(e => e.UserId == userId)
                    .OrderBy(e => e.StartDate)
                    .ToListAsync();
            }
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Pobierz pierwszy błąd walidacji i wyświetl toast z błędem
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                if (!string.IsNullOrEmpty(firstError))
                {
                    TempData["ToastMessage"] = firstError;
                    TempData["ToastType"] = "danger";
                }
                await OnGetAsync();
                return Page();
            }

            if (EndDate <= StartDate)
            {
                ModelState.AddModelError(nameof(EndDate), "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.");
                await OnGetAsync();
                return Page();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Pobranie wybranego wydarzenia
            var selectedEvent = await _context.Events.FindAsync(EventId);
            if (selectedEvent == null)
            {
                ModelState.AddModelError(nameof(EventId), "Wybrane wydarzenie nie istnieje.");
                await OnGetAsync();
                return Page();
            }

            // Walidacja daty zadania w zakresie wydarzenia
            else if (StartDate < selectedEvent.StartDate || EndDate > selectedEvent.EndDate)
            {
                ModelState.AddModelError(nameof(EventId),
                    "Zadanie musi zawierać się w zakresie wybranego wydarzenia.");
                await OnGetAsync();
                return Page();
            }

            var newTask = new TaskItem
            {
                Title = Title!,
                Description = Description ?? "",
                StartDate = StartDate,
                EndDate = EndDate,
                IsDone = false,
                ScrumEventId = EventId,
                UserId = userId
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            // Pobierz nazwę użytkownika
            var user = await _context.Users.FindAsync(userId);

            var taskDto = new
            {
                id = newTask.Id,
                title = newTask.Title,
                description = newTask.Description,
                startDate = newTask.StartDate.ToString("yyyy-MM-dd HH:mm"),
                endDate = newTask.EndDate.ToString("yyyy-MM-dd HH:mm"),
                isDone = newTask.IsDone,
                userName = user?.UserName ?? "",
                canEdit = true,   // lub logika np. User.IsInRole("Admin")
                canDelete = true  // podobnie
            };

            if (!User.IsInRole("Admin"))
            {
                // Zwykły user – wysyłamy event do wszystkich (żeby toast był widoczny)
                await _hubContext.Clients.All.SendAsync("TaskAdded", taskDto);
            }
            else
            {
                // Admin – nie wysyłamy eventu, żeby nie pojawiał się toast u userów
            }

            TempData["ToastMessage"] = "Dodano nowe zadanie!";
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostToggleDoneAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Pobierz zadanie wraz z użytkownikiem
            var task = await _context.Tasks.Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.Id == id && (User.IsInRole("Admin") || t.UserId == userId));

            if (task == null)
                return NotFound();

            // Zmień status
            task.IsDone = !task.IsDone;
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();

            // DTO dla admina
            var taskAdminDto = new
            {
                task.Id,
                task.Title,
                task.Description,
                StartDate = task.StartDate.ToString("yyyy-MM-dd HH:mm"),
                EndDate = task.EndDate.ToString("yyyy-MM-dd HH:mm"),
                task.IsDone,
                UserName = task.User?.UserName ?? "",
                CanEdit = true,
                CanDelete = true
            };

            // DTO dla zwykłego użytkownika
            var taskUserDto = new
            {
                task.Id,
                task.Title,
                task.Description,
                StartDate = task.StartDate.ToString("yyyy-MM-dd HH:mm"),
                EndDate = task.EndDate.ToString("yyyy-MM-dd HH:mm"),
                task.IsDone,
                CanEdit = true,
                CanDelete = true
            };

            // Wyślij adminom DTO z kolumną UserName
            var adminIds = _context.UserRoles
                            .Where(ur => ur.RoleId == "98954494-ef5f-4a06-87e4-22ef31417c9c")
                            .Select(ur => ur.UserId)
                            .ToList();

            if (adminIds.Any())
            {
                await _hubContext.Clients.Users(adminIds).SendAsync("TaskUpdated", taskAdminDto);
            }

            // Wyślij wszystkim innym użytkownikom (czyli zwykłym userom) DTO bez kolumny UserName
            var userIds = _context.Users
                            .Where(u => !adminIds.Contains(u.Id))
                            .Select(u => u.Id)
                            .ToList();

            if (userIds.Any())
            {
                await _hubContext.Clients.Users(userIds).SendAsync("TaskUpdated", taskUserDto);
            }

            // Toast dla wykonującego akcję
            TempData["ToastMessage"] = "Status zadania został zmieniony";
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = User.IsInRole("Admin")
                ? await _context.Tasks.FirstOrDefaultAsync(e => e.Id == id)
                : await _context.Tasks.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (task == null)
                return NotFound();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            // Jeżeli event nie należy do konkretnego admina, wyślij do wszystkich
            if (task.UserId != "fe2c4ac1-87bd-4fef-9f91-954547d7d4f1")
            {
                await _hubContext.Clients.All.SendAsync("TaskDeleted", task.Id);
            }

            TempData["ToastMessage"] = "Zadanie zostało usunięte";
            TempData["ToastType"] = "danger";

            return RedirectToPage();
        }
    }
}
