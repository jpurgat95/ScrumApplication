using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public EditEventModel(ScrumDbContext context)
        {
            _context = context;
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

            TempData["ToastMessage"] = "Wydarzenie zostało zaktualizowane";
            TempData["ToastType"] = "success";

            return RedirectToPage("/Events/Index");
        }
    }
}
