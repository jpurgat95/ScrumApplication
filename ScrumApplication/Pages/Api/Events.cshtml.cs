using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data; // namespace Twojego DbContext
using System.Linq;
using System.Security.Claims;

namespace ScrumApplication.Pages.Api;
[Authorize] // tylko zalogowani użytkownicy mogą uzyskać dostęp do tego API
public class EventsModel : PageModel
{
    private readonly ScrumDbContext _context;

    public EventsModel(ScrumDbContext context)
    {
        _context = context;
    }

    public IActionResult OnGet()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //Jeśli użytkownik jest administratorem, pobierz wszystkie wydarzenia
            var tasksQuery = User.IsInRole("Admin")
                ? _context.Events
                : _context.Events.Where(e => e.UserId == userId);

            var events = tasksQuery
                .Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    description = e.Description,
                    start = e.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = e.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    isDone = e.IsDone,
                    userId = e.UserId,
                    userName = User.IsInRole("Admin") ? e.User.UserName : null
                })
                .ToList();

            return new JsonResult(events);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
    public IActionResult OnPostToggleDone(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // admin może zmieniać każde wydarzenie, user tylko swoje
            var ev = User.IsInRole("Admin")
                ? _context.Events.FirstOrDefault(e => e.Id == id)
                : _context.Events.FirstOrDefault(e => e.Id == id && e.UserId == userId);

            if (ev != null)
            {
                ev.IsDone = !ev.IsDone;
                _context.SaveChanges();
                return new JsonResult(new { success = true, isDone = ev.IsDone });
            }
            return new JsonResult(new { success = false });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
}
