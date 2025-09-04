using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data;
using System.Security.Claims;

namespace ScrumApplication.Pages.Api;
[Authorize] //tylko zalogowani użytkownicy mogą uzyskać dostęp do tego API
public class TasksModel : PageModel
{
    private readonly ScrumDbContext _context;

    public TasksModel(ScrumDbContext context)
    {
        _context = context;
    }
    public IActionResult OnGet()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //Jeśli użytkownik jest administratorem, pobierz wszystkie zadania
        var tasksQuery = User.IsInRole("Admin") 
            ? _context.Tasks 
            : _context.Tasks.Where(t => t.UserId == userId);

        var tasks = tasksQuery
            .Select(t => new
            {
                id = t.Id,
                title = t.Title,
                description = t.Description,
                start = t.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = t.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                isDone = t.IsDone,
                userId = t.UserId,
                userName = User.IsInRole("Admin") ? t.User.UserName : null
            })
            .ToList();

        return new JsonResult(tasks);
    }
    public IActionResult OnPostToggleDone(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // admin może zmieniać każde zadanie, user tylko swoje
        var task = User.IsInRole("Admin")
            ? _context.Tasks.FirstOrDefault(t => t.Id == id)
            : _context.Tasks.FirstOrDefault(t => t.Id == id && t.UserId == userId);

        if (task != null)
        {
            task.IsDone = !task.IsDone;
            _context.SaveChanges();
            return new JsonResult(new { success = true, isDone = task.IsDone });
        }
        return new JsonResult(new { success = false });
    }
}
