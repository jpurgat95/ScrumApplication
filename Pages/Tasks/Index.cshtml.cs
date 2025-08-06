using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Models;
using ScrumApplication.Data;

namespace ScrumApplication.Pages.Tasks;

public class IndexModel : PageModel
{
    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public DateTime Date { get; set; } = DateTime.Now;

    public List<TaskItem> Tasks { get; set; } = new();

    public void OnGet()
    {
        Tasks = FakeDb.GetTasks();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            Tasks = FakeDb.GetTasks();
            return Page();
        }

        var newTask = new TaskItem
        {
            Title = Title,
            Description = Description,
            Date = Date,
            IsDone = false
        };

        FakeDb.AddTask(newTask);

        TempData["ToastMessage"] = "Zadanie zostało utworzone";

        return RedirectToPage();
    }

    public IActionResult OnPostToggleDone(int id)
    {
        var task = FakeDb.GetTaskById(id);
        if (task != null)
        {
            task.IsDone = !task.IsDone;
            TempData["ToastMessage"] = task.IsDone ? "Zadanie oznaczone jako wykonane" : "Zadanie oznaczone jako niewykonane";
            TempData["ToastType"] = task.IsDone ? "success" : "warning"; 
        }
        return RedirectToPage();
    }


    public IActionResult OnPostDelete(int id, string type)
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
