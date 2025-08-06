using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data;
using ScrumApplication.Models;

namespace ScrumApplication.Pages.Events;

public class IndexModel : PageModel
{
    [BindProperty]
    public string? Title { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public DateTime Date { get; set; } = DateTime.Now;

    public List<ScrumEvent> Events => FakeDb.GetEvents();

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Title))
            return Page();

        Date = Date.Date; // tylko data, bez godziny

        FakeDb.AddEvent(new ScrumEvent
        {
            Title = Title!,
            Description = Description ?? "",
            Date = Date
        });

        TempData["ToastMessage"] = "Dodano nowe wydarzenie!";
        TempData["ToastType"] = "success";

        return RedirectToPage();
    }


    public IActionResult OnPostToggleDone(int id)
    {
        var ev = FakeDb.GetEventById(id);
        if (ev != null)
        {
            ev.IsDone = !ev.IsDone;
            FakeDb.UpdateEvent(ev);

            TempData["ToastMessage"] = ev.IsDone ? "Wydarzenie oznaczone jako wykonane" : "Wydarzenie oznaczone jako niewykonane";
            TempData["ToastType"] = ev.IsDone ? "success" : "warning";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(string type, int id)
    {
        if (type == "event")
        {
            FakeDb.RemoveEvent(id);
            TempData["ToastMessage"] = "Wydarzenie zostało usunięte";
            TempData["ToastType"] = "danger";
        }

        return RedirectToPage();
    }
}
