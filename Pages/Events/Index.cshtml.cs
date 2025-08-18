using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ScrumApplication.Pages.Events
{
    public class IndexModel : PageModel
    {
        private readonly ScrumDbContext _context;
        private readonly IHubContext<TaskHub> _hubContext;

        public IndexModel(ScrumDbContext context, IHubContext<TaskHub> hubContext)
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
            Events = await _context.Events.OrderBy(e => e.StartDate).ToListAsync();
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
                //TempData["ToastMessage"] = "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.";
                //TempData["ToastType"] = "danger";
                await OnGetAsync();
                return Page(); 
            }

            var newEvent = new ScrumEvent
            {
                Title = Title!,
                Description = Description ?? "",
                StartDate = StartDate,
                EndDate = EndDate,
                IsDone = false
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Dodano nowe wydarzenie!";
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleDoneAsync(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev != null)
            {
                ev.IsDone = !ev.IsDone;
                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = ev.IsDone ? "Wydarzenie oznaczone jako wykonane" : "Wydarzenie oznaczone jako niewykonane";
                TempData["ToastType"] = ev.IsDone ? "success" : "warning";

                // Tutaj wysyłamy sygnał do wszystkich klientów SignalR
                await _hubContext.Clients.All.SendAsync("TaskUpdated");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string type, int id)
        {
            if (type == "event")
            {
                var ev = await _context.Events.FindAsync(id);
                if (ev != null)
                {
                    _context.Events.Remove(ev);
                    await _context.SaveChangesAsync();

                    TempData["ToastMessage"] = "Wydarzenie zostało usunięte";
                    TempData["ToastType"] = "danger";
                }
            }

            return RedirectToPage();
        }
    }
}
