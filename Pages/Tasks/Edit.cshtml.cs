using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;

namespace ScrumApplication.Pages.Tasks
{
    public class EditModel : PageModel
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
            var task = FakeDb.GetTaskById(id);
            if (task == null)
                return NotFound();

            Id = task.Id;
            Title = task.Title;
            Description = task.Description;
            StartDate = task.StartDate;
            EndDate = task.EndDate;

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

            var task = FakeDb.GetTaskById(Id);
            if (task == null)
                return NotFound();

            task.Title = Title!;
            task.Description = Description!;
            task.StartDate = StartDate;
            task.EndDate = EndDate;

            FakeDb.UpdateTask(task);

            TempData["ToastMessage"] = "Zadanie zostało zaktualizowane";
            TempData["ToastType"] = "success";

            return RedirectToPage("/Tasks/Index");
        }
    }
}
