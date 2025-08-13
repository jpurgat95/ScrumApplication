using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data; // namespace Twojego DbContext
using System.Linq;

namespace ScrumApplication.Pages.Api;

public class EventsModel : PageModel
{
    private readonly ScrumDbContext _context;

    public EventsModel(ScrumDbContext context)
    {
        _context = context;
    }

    public IActionResult OnGet()
    {
        var events = _context.Events
            .Select(e => new
            {
                id = e.Id,
                title = e.Title,
                description = e.Description,
                start = e.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = e.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                isDone = e.IsDone
            })
            .ToList();

        return new JsonResult(events);
    }
    public IActionResult OnPostToggleDone(int id)
    {
        var ev = _context.Events.FirstOrDefault(e => e.Id == id);
        if (ev != null)
        {
            ev.IsDone = !ev.IsDone;
            _context.SaveChanges();
            return new JsonResult(new { success = true, isDone = ev.IsDone });
        }
        return new JsonResult(new { success = false });
    }
}
