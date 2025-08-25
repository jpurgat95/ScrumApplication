using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ScrumApplication.Pages.Tasks
{
    public class EditModel : PageModel
    {
        private readonly ScrumDbContext _context;
        private readonly IHubContext<UpdatesHub> _hubContext;

        public EditModel(ScrumDbContext context, IHubContext<UpdatesHub> hubContext) 
        {
            _context = context;
            _hubContext = hubContext;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Tytuł jest wymagany")]
        public string? Title { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Opis jest wymagany")]
        public string? Description { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Data rozpoczęcia jest wymagana")]
        public DateTime StartDate { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Data zakończenia jest wymagana")]
        public DateTime EndDate { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = User.IsInRole("Admin")
                ? await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id)
                : await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return NotFound();

            Id = task.Id;
            Title = task.Title;
            Description = task.Description;
            StartDate = task.StartDate;
            EndDate = task.EndDate;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (EndDate <= StartDate)
            {
                ModelState.AddModelError(nameof(EndDate), "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.");
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Szukamy wydarzenia, ale użytkownik widzi tylko swoje (chyba że Admin)
            var task = User.IsInRole("Admin")
                ? await _context.Tasks.FirstOrDefaultAsync(t => t.Id == Id)
                : await _context.Tasks.FirstOrDefaultAsync(t => t.Id == Id && t.UserId == userId);

            if (task == null)
                return NotFound();

            task.Title = Title!;
            task.Description = Description!;
            task.StartDate = StartDate;
            task.EndDate = EndDate;

            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            // DTO dla admina
            var taskAdminDto = new
            {
                task.Id,
                task.Title,
                task.Description,
                StartDate = task.StartDate.ToString("yyyy-MM-dd HH:mm"),
                EndDate = task.EndDate.ToString("yyyy-MM-dd HH:mm"),
                task.IsDone,
                UserName = task.User?.UserName ?? "",
                CanEdit = true,
                CanDelete = true
            };

            // DTO dla zwykłego użytkownika
            var taskUserDto = new
            {
                task.Id,
                task.Title,
                task.Description,
                StartDate = task.StartDate.ToString("yyyy-MM-dd HH:mm"),
                EndDate = task.EndDate.ToString("yyyy-MM-dd HH:mm"),
                task.IsDone,
                CanEdit = true,
                CanDelete = true
            };

            // Wyślij adminom DTO z kolumną UserName
            var adminIds = _context.UserRoles
                            .Where(ur => ur.RoleId == "98954494-ef5f-4a06-87e4-22ef31417c9c")
                            .Select(ur => ur.UserId)
                            .ToList();

            if (adminIds.Any())
            {
                await _hubContext.Clients.Users(adminIds).SendAsync("TaskUpdated", taskAdminDto);
            }

            // Wyślij wszystkim innym użytkownikom (czyli zwykłym userom) DTO bez kolumny UserName
            var userIds = _context.Users
                            .Where(u => !adminIds.Contains(u.Id))
                            .Select(u => u.Id)
                            .ToList();

            if (userIds.Any())
            {
                await _hubContext.Clients.Users(userIds).SendAsync("TaskUpdated", taskUserDto);
            }

            //var task = await _context.Tasks.FindAsync(Id);
            //if (task == null)
            //    return NotFound();

            //task.Title = Title!;
            //task.Description = Description!;
            //task.StartDate = StartDate;
            //task.EndDate = EndDate;

            //_context.Tasks.Update(task);
            //await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Zadanie zostało zaktualizowane";
            TempData["ToastType"] = "success";

            return RedirectToPage("/Tasks/Index");
        }
    }
}
