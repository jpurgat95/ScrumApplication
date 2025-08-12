using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;

namespace ScrumApplication.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ScrumDbContext _context;

        public IndexModel(ScrumDbContext context) 
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Tytuł jest wymagany")]
        public string? Title { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Opis jest wymagany")]
        public string? Description { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Data rozpoczęcia jest wymagana")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [BindProperty]
        [Required(ErrorMessage = "Data zakończenia jest wymagana")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddHours(1);

        public List<TaskItem> Tasks = new();

        public async Task OnGetAsync()
        {
            Tasks = await _context.Tasks.OrderBy(task => task.StartDate).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                if (!string.IsNullOrEmpty(firstError))
                {
                    TempData["ToastMessage"] = firstError;
                    TempData["ToastType"] = "danger";
                }
                await OnGetAsync();
                return Page();
            }

            if (EndDate <= StartDate)
            {
                ModelState.AddModelError(nameof(EndDate), "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.");
                await OnGetAsync();
                return Page();
            }

            var newTask = new TaskItem
            {
                Title = Title!,
                Description = Description ?? "",
                StartDate = StartDate,
                EndDate = EndDate,
                IsDone = false
            };
            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Dodano nowe zadanie!";
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleDoneAsync(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                task.IsDone = !task.IsDone;
                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = task.IsDone ? "Zadanie oznaczone jako wykonane" : "Zadanie oznaczone jako niewykonane";
                TempData["ToastType"] = task.IsDone ? "success" : "warning";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string type, int id)
        {
            if (type == "task")
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task != null)
                {
                    _context.Tasks.Remove(task);
                    await _context.SaveChangesAsync();

                    TempData["ToastMessage"] = "Zadanie zostało usunięte";
                    TempData["ToastType"] = "danger";
                }
            }

            return RedirectToPage();
        }
    }
}
