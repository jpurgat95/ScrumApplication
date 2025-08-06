using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScrumApplication.Data;
using ScrumApplication.Models;

namespace ScrumApplication.Pages.Events
{
    public class EditModel : PageModel
    {
        [BindProperty]
        public ScrumEvent Event { get; set; } = new ScrumEvent();

        public IActionResult OnGet(int id)
        {
            var ev = FakeDb.GetEventById(id);
            if (ev == null)
            {
                return NotFound();
            }
            Event = ev;
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            FakeDb.UpdateEvent(Event);
            TempData["ToastMessage"] = "Wydarzenie zostało zaktualizowane";
            return RedirectToPage("/Events/Index");
        }
    }
}
