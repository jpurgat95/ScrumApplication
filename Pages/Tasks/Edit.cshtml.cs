using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Models;
using ScrumApplication.Data;

namespace ScrumApplication.Pages.Tasks
{
    public class EditModel : PageModel
    {
        [BindProperty]
        public TaskItem Task { get; set; } = new TaskItem();

        public IActionResult OnGet(int id)
        {
            var task = FakeDb.GetTaskById(id);
            if (task == null)
            {
                return NotFound();
            }
            Task = task;
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            FakeDb.UpdateTask(Task);
            TempData["ToastMessage"] = "Zadanie zostało zaktualizowane.";
            TempData["ToastType"] = "success";
            return RedirectToPage("/Tasks/Index");
        }
    }
}
