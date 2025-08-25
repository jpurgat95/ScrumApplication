using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ScrumApplication.Pages.Events
{
    public class EditEventModel : PageModel
    {
        private readonly ScrumDbContext _context;
        private readonly IHubContext<UpdatesHub> _hubContext;

        public EditEventModel(ScrumDbContext context, IHubContext<UpdatesHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Tytuł jest wymagany")]
        public string? Title { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Opis jest wymagany")]
        public string? Description { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Data rozpoczęcia jest wymagana")]
        public DateTime StartDate { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Data zakończenia jest wymagana")]
        public DateTime EndDate { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Pobieramy wydarzenie, ale filtrujemy wg roli
            var ev = User.IsInRole("Admin")
                ? await _context.Events.FirstOrDefaultAsync(e => e.Id == id)
                : await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (ev == null)
                return NotFound();

            Id = ev.Id;
            Title = ev.Title;
            Description = ev.Description;
            StartDate = ev.StartDate;
            EndDate = ev.EndDate;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (EndDate <= StartDate)
            {
                ModelState.AddModelError(nameof(EndDate), "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.");
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Szukamy wydarzenia, ale użytkownik widzi tylko swoje (chyba że Admin)
            var ev = User.IsInRole("Admin")
                ? await _context.Events.FirstOrDefaultAsync(e => e.Id == Id)
                : await _context.Events.FirstOrDefaultAsync(e => e.Id == Id && e.UserId == userId);

            if (ev == null)
                return NotFound();

            ev.Title = Title!;
            ev.Description = Description!;
            ev.StartDate = StartDate;
            ev.EndDate = EndDate;

            _context.Events.Update(ev);
            await _context.SaveChangesAsync();
            // DTO dla admina
            var eventAdminDto = new
            {
                ev.Id,
                ev.Title,
                ev.Description,
                StartDate = ev.StartDate.ToString("yyyy-MM-dd HH:mm"),
                EndDate = ev.EndDate.ToString("yyyy-MM-dd HH:mm"),
                ev.IsDone,
                UserName = ev.User?.UserName ?? "",
                CanEdit = true,
                CanDelete = true
            };

            // DTO dla zwykłego użytkownika
            var eventUserDto = new
            {
                ev.Id,
                ev.Title,
                ev.Description,
                StartDate = ev.StartDate.ToString("yyyy-MM-dd HH:mm"),
                EndDate = ev.EndDate.ToString("yyyy-MM-dd HH:mm"),
                ev.IsDone,
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
                await _hubContext.Clients.Users(adminIds).SendAsync("EventUpdated", eventAdminDto);
            }

            // Wyślij wszystkim innym użytkownikom (czyli zwykłym userom) DTO bez kolumny UserName
            var userIds = _context.Users
                            .Where(u => !adminIds.Contains(u.Id))
                            .Select(u => u.Id)
                            .ToList();

            if (userIds.Any())
            {
                await _hubContext.Clients.Users(userIds).SendAsync("EventUpdated", eventUserDto);
            }

            TempData["ToastMessage"] = "Wydarzenie zostało zaktualizowane";
            TempData["ToastType"] = "success";

            return RedirectToPage("/Events/Index");
        }
    }
}
