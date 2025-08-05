using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data;
using ScrumApplication.Models;

namespace ScrumApplication.Pages
{
    public class IndexModel : PageModel
    {
        public List<TaskItem> Tasks { get; set; } = new();

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string Description { get; set; } = string.Empty;

        public void OnGet()
        {
            Tasks = FakeDb.GetTasks();
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                ModelState.AddModelError("Title", "Tytu³ jest wymagany");
                Tasks = FakeDb.GetTasks();
                return Page();
            }

            var newTask = new TaskItem
            {
                Title = Title,
                Description = Description,
                IsDone = false
            };

            FakeDb.AddTask(newTask);

            return RedirectToPage(); // odœwie¿a stronê po dodaniu
        }
        public IActionResult OnPostDelete(int id)
        {
            FakeDb.RemoveTask(id);
            TempData["Deleted"] = true;
            return RedirectToPage();
        }
    }
}
