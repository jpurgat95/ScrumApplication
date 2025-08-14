using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data;

namespace ScrumApplication.Pages.Api;

public class TasksModel : PageModel
{
    private readonly ScrumDbContext _context;

    public TasksModel(ScrumDbContext context)
    {
        _context = context;
    }
    public IActionResult OnGet()
    {
        var tasks = _context.Tasks
            .Select(t => new
            {
                id = t.Id,
                title = t.Title,
                description = t.Description,
                start = t.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = t.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                isDone = t.IsDone
            })
            .ToList();
        return new JsonResult(tasks);
    }
    public IActionResult OnPostToggleDone(int id)
    {
        var task = _context.Tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.IsDone = !task.IsDone;
            _context.SaveChanges();
            return new JsonResult(new { success = true, isDone = task.IsDone });
        }
        return new JsonResult(new { success = false });
    }
}
