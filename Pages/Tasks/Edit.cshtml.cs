using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ScrumApplication.Pages.Tasks
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IHubContext<UpdatesHub> _hubContext;

        public EditModel(
            ITaskRepository taskRepository,
            IUserRoleRepository userRoleRepository,
            IHubContext<UpdatesHub> hubContext)
        {
            _taskRepository = taskRepository;
            _userRoleRepository = userRoleRepository;
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
            var isAdmin = User.IsInRole("Admin");

            var task = await _taskRepository.GetTaskByIdAsync(id, userId, isAdmin);
            if (task == null)
                return NotFound();

            // Sprawdź czy powiązane wydarzenie jest wykonane
            if (task.ScrumEvent != null && task.ScrumEvent.IsDone)
            {
                TempData["ToastMessage"] = "Nie można edytować zadania, ponieważ powiązane wydarzenie zostało oznaczone jako wykonane.";
                TempData["ToastType"] = "warning";
                return RedirectToPage("/Tasks/Index");
            }

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
                return Page();

            if (EndDate <= StartDate)
            {
                ModelState.AddModelError(nameof(EndDate), "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.");
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var task = await _taskRepository.GetTaskByIdAsync(Id, userId, isAdmin);
            if (task == null)
                return NotFound();

            task.Title = Title!;
            task.Description = Description!;
            task.StartDate = StartDate;
            task.EndDate = EndDate;

            await _taskRepository.UpdateTaskAsync(task);

            var adminIds = await _userRoleRepository.GetUserIdsInRoleAsync("98954494-ef5f-4a06-87e4-22ef31417c9c");
            var userIds = await _userRoleRepository.GetUserIdsNotInRolesAsync(adminIds);

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

            if (adminIds.Count > 0)
                await _hubContext.Clients.Users(adminIds).SendAsync("TaskUpdated", taskAdminDto);

            if (userIds.Count > 0)
                await _hubContext.Clients.Users(userIds).SendAsync("TaskUpdated", taskUserDto);

            TempData["ToastMessage"] = "Zadanie zostało zaktualizowane";
            TempData["ToastType"] = "success";

            return RedirectToPage("/Tasks/Index");
        }
    }
}
