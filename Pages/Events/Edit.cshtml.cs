using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;

namespace ScrumApplication.Pages.Events
{
    public class EditEventModel : PageModel
    {
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

        public IActionResult OnGet(int id)
        {
            var ev = FakeDb.GetEventById(id);
            if (ev == null)
                return NotFound();

            Id = ev.Id;
            Title = ev.Title;
            Description = ev.Description;
            StartDate = ev.StartDate;
            EndDate = ev.EndDate;

            return Page();
        }

        public IActionResult OnPost()
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

            var ev = FakeDb.GetEventById(Id);
            if (ev == null)
                return NotFound();

            ev.Title = Title!;
            ev.Description = Description!;
            ev.StartDate = StartDate;
            ev.EndDate = EndDate;

            FakeDb.UpdateEvent(ev);

            TempData["ToastMessage"] = "Wydarzenie zostało zaktualizowane";
            TempData["ToastType"] = "success";

            return RedirectToPage("/Events/Index");
        }
    }
}
