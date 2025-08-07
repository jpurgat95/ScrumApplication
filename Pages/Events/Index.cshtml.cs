using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;

namespace ScrumApplication.Pages.Events
{
    public class IndexModel : PageModel
    {
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

        public List<ScrumEvent> Events => FakeDb.GetEvents();

        public void OnGet()
        {
        }

        public IActionResult OnPost()
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
                return Page();
            }

            if (EndDate <= StartDate)
            {
                ModelState.AddModelError(nameof(EndDate), "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.");
                TempData["ToastMessage"] = "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.";
                TempData["ToastType"] = "danger";
                return Page();
            }

            FakeDb.AddEvent(new ScrumEvent
            {
                Title = Title!,
                Description = Description ?? "",
                StartDate = StartDate,
                EndDate = EndDate
            });

            TempData["ToastMessage"] = "Dodano nowe wydarzenie!";
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }

        public IActionResult OnPostToggleDone(int id)
        {
            var ev = FakeDb.GetEventById(id);
            if (ev != null)
            {
                ev.IsDone = !ev.IsDone;
                FakeDb.UpdateEvent(ev);

                TempData["ToastMessage"] = ev.IsDone ? "Wydarzenie oznaczone jako wykonane" : "Wydarzenie oznaczone jako niewykonane";
                TempData["ToastType"] = ev.IsDone ? "success" : "warning";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(string type, int id)
        {
            if (type == "event")
            {
                FakeDb.RemoveEvent(id);
                TempData["ToastMessage"] = "Wydarzenie zostało usunięte";
                TempData["ToastType"] = "danger";
            }

            return RedirectToPage();
        }
    }
}
