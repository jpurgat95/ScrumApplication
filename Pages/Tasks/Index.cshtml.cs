using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;

namespace ScrumApplication.Pages.Tasks
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

        public List<TaskItem> Tasks => FakeDb.GetTasks();

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
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

            FakeDb.AddTask(new TaskItem
            {
                Title = Title!,
                Description = Description ?? "",
                StartDate = StartDate,
                EndDate = EndDate
            });

            TempData["ToastMessage"] = "Dodano nowe zadanie!";
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }

        public IActionResult OnPostToggleDone(int id)
        {
            var task = FakeDb.GetTaskById(id);
            if (task != null)
            {
                task.IsDone = !task.IsDone;
                FakeDb.UpdateTask(task);

                TempData["ToastMessage"] = task.IsDone ? "Zadanie oznaczone jako wykonane" : "Zadanie oznaczone jako niewykonane";
                TempData["ToastType"] = task.IsDone ? "success" : "warning";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(string type, int id)
        {
            if (type == "task")
            {
                FakeDb.RemoveTask(id);
                TempData["ToastMessage"] = "Zadanie zostało usunięte";
                TempData["ToastType"] = "danger";
            }

            return RedirectToPage();
        }
    }
}
