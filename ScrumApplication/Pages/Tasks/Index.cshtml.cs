using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ScrumApplication.Pages.Tasks
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IHubContext<UpdatesHub> _hubContext;

        public IndexModel(
            ITaskRepository taskRepository,
            IEventRepository eventRepository,
            IUserRoleRepository userRoleRepository,
            IHubContext<UpdatesHub> hubContext)
        {
            _taskRepository = taskRepository;
            _eventRepository = eventRepository;
            _userRoleRepository = userRoleRepository;
            _hubContext = hubContext;
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

        [BindProperty]
        [Required(ErrorMessage = "Wybór wydarzenia jest wymagany")]
        public int EventId { get; set; }

        public List<TaskItem> Tasks { get; set; } = new();

        public List<ScrumEvent> Events { get; set; } = new();
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                Tasks = await _taskRepository.GetTasksAsync(userId, isAdmin);
                Events = await _eventRepository.GetEventsAsync(userId, isAdmin);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
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

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                // Walidacja wydarzenia
                var selectedEvent = await _eventRepository.GetEventByIdAsync(EventId, userId, isAdmin: User.IsInRole("Admin"));
                if (selectedEvent == null)
                {
                    ModelState.AddModelError(nameof(EventId), "Wybrane zadanie nie istnieje.");
                    await OnGetAsync();
                    return Page();
                }

                if (StartDate < selectedEvent.StartDate || EndDate > selectedEvent.EndDate)
                {
                    ModelState.AddModelError(nameof(EventId), "Zadanie musi mieścić się w zakresie wybranego wydarzenia.");
                    await OnGetAsync();
                    return Page();
                }

                var newTask = new TaskItem
                {
                    Title = Title!,
                    Description = Description ?? "",
                    StartDate = StartDate,
                    EndDate = EndDate,
                    IsDone = false,
                    ScrumEventId = EventId,
                    UserId = userId
                };

                await _taskRepository.AddTaskAsync(newTask);
                var adminIds = await _userRoleRepository.GetUserIdsInRoleAsync("98954494-ef5f-4a06-87e4-22ef31417c9c");
                var userIds = await _userRoleRepository.GetUserIdsNotInRolesAsync(adminIds);
                var currentUserId = User.Identity?.Name ?? "";

                var taskDto = new
                {
                    newTask.Id,
                    newTask.Title,
                    newTask.Description,
                    StartDate = newTask.StartDate.ToString("yyyy-MM-dd HH:mm"),
                    EndDate = newTask.EndDate.ToString("yyyy-MM-dd HH:mm"),
                    newTask.IsDone,
                    UserName = currentUserId,
                    CanEdit = true,
                    CanDelete = true
                };

                if (!isAdmin)
                {
                    await _hubContext.Clients.Users(adminIds).SendAsync("TaskAdded", taskDto);
                }

                TempData["ToastMessage"] = "Dodano nowe zadanie!";
                TempData["ToastType"] = "success";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Wystąpił błąd podczas próby dodania zadania: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostToggleDoneAsync(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                var task = await _taskRepository.GetTaskByIdAsync(id, userId, isAdmin);
                if (task == null)
                    return NotFound();

                task.IsDone = !task.IsDone;
                await _taskRepository.UpdateTaskAsync(task);

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
                var adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c"; // id roli admina

                var adminIds = await _userRoleRepository.GetUserIdsInRoleAsync(adminRoleId);
                var userToSendId = task.UserId;
                if (task.UserId == "fe2c4ac1-87bd-4fef-9f91-954547d7d4f1")
                {
                    await _hubContext.Clients.Users(adminIds).SendAsync("TaskUpdated", taskAdminDto);
                }
                else
                {
                    await _hubContext.Clients.Users(adminIds).SendAsync("TaskUpdated", taskAdminDto);
                    await _hubContext.Clients.User(userToSendId).SendAsync("TaskUpdated", taskUserDto);
                }

                TempData["ToastMessage"] = "Status zadania został zmieniony";
                TempData["ToastType"] = "success";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Wystąpił błąd podczas próby zmiany statusu zadania: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");


                var task = await _taskRepository.GetTaskByIdAsync(id, userId, isAdmin);
                if (task == null)
                    return NotFound();

                var adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c"; // id roli admina
                var adminIds = await _userRoleRepository.GetUserIdsInRoleAsync(adminRoleId);
                await _taskRepository.DeleteTaskAsync(task);
                var userToSendId = task.UserId;

                if (isAdmin)
                {
                    await _hubContext.Clients.User(userToSendId).SendAsync("TaskDeleted", task.Id);
                    await _hubContext.Clients.Users(adminIds).SendAsync("BlockEditWhenTaskDeleted", task.Id);
                    await _hubContext.Clients.User(userToSendId).SendAsync("BlockEditWhenTaskDeleted", task.Id);
                }
                else
                {
                    await _hubContext.Clients.Users(adminIds).SendAsync("TaskDeleted", task.Id);
                    await _hubContext.Clients.Users(adminIds).SendAsync("BlockEditWhenTaskDeleted", task.Id);
                    await _hubContext.Clients.User(userToSendId).SendAsync("BlockEditWhenTaskDeleted", task.Id);
                }

                TempData["ToastMessage"] = "Zadanie zostało usunięte";
                TempData["ToastType"] = "danger";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Wystąpił błąd podczas próby usunięcia zadania: {ex.Message}");
                return Page();
            }
        }
    }
}
