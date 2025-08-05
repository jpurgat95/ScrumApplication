using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data;
using ScrumApplication.Models;

namespace ScrumApplication.Pages
{
    public class EditTaskModel : PageModel
    {
        [BindProperty]
        public TaskItem Task { get; set; } = new TaskItem();

        public IActionResult OnGet(int id)
        {
            var existing = FakeDb.GetTaskById(id);
            if (existing == null) return RedirectToPage("Index");

            Task = new TaskItem
            {
                Id = existing.Id,
                Title = existing.Title,
                Description = existing.Description,
                IsDone = existing.IsDone,
                CreatedAt = existing.CreatedAt
            };

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            FakeDb.UpdateTask(Task);
            return RedirectToPage("Index");
        }
    }
}
