using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ScrumApplication.Pages.Events
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

        public List<ScrumEvent> Events = new();

        public async Task OnGetAsync()
        {
            if (User.IsInRole("Admin"))
            {
                Events = await _context.Events
                    .Include(e => e.User) // <-- to ładuje pełnego użytkownika
                    .OrderBy(e => e.StartDate)
                    .ToListAsync();
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Events = await _context.Events
                    .Include(e => e.User)
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

            var newEvent = new ScrumEvent
            {
                Title = Title!,
                Description = Description ?? "",
                StartDate = StartDate,
                EndDate = EndDate,
                IsDone = false,
                UserId = userId
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            // Pobierz nazwę użytkownika
            var user = await _context.Users.FindAsync(userId);

            var eventDto = new
            {
                id = newEvent.Id,
                title = newEvent.Title,
                description = newEvent.Description,
                startDate = newEvent.StartDate.ToString("yyyy-MM-dd HH:mm"),
                endDate = newEvent.EndDate.ToString("yyyy-MM-dd HH:mm"),
                isDone = newEvent.IsDone,
                userName = user?.UserName ?? "",
                canEdit = true,   // lub logika np. User.IsInRole("Admin")
                canDelete = true  // podobnie
            };

            await _hubContext.Clients.All.SendAsync("EventAdded", eventDto);

            TempData["ToastMessage"] = "Dodano nowe wydarzenie!";
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleDoneAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var ev = User.IsInRole("Admin")
                ? await _context.Events.FirstOrDefaultAsync(e => e.Id == id)
                : await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (ev == null)
                return NotFound();

            ev.IsDone = !ev.IsDone;

            _context.Events.Update(ev);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("EventAdded", ev);

            TempData["ToastMessage"] = "Status wydarzenia został zmieniony";
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var ev = User.IsInRole("Admin")
                ? await _context.Events.FirstOrDefaultAsync(e => e.Id == id)
                : await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (ev == null)
                return NotFound();

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("EventDeleted", ev);

            TempData["ToastMessage"] = "Wydarzenie zostało usunięte";
            TempData["ToastType"] = "danger";

            return RedirectToPage();

        }
    }
}
