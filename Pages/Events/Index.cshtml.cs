using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using ScrumApplication.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ScrumApplication.Pages.Events
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IEventRepository _eventRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IHubContext<UpdatesHub> _hubContext;
        private readonly IUserRoleRepository _userRoleRepository;

        public IndexModel(IEventRepository eventRepository, ITaskRepository taskRepository, IHubContext<UpdatesHub> hubContext, IUserRoleRepository userRoleRepository)
        {
            _eventRepository = eventRepository;
            _taskRepository = taskRepository;
            _hubContext = hubContext;
            _userRoleRepository = userRoleRepository;
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

        public List<ScrumEvent> Events { get; set; } = new();

        public List<TaskItem> Tasks { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            Events = await _eventRepository.GetEventsAsync(userId, isAdmin);
            Tasks = await _taskRepository.GetTasksAsync(userId, isAdmin);
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var newEvent = new ScrumEvent
            {
                Title = Title!,
                Description = Description ?? "",
                StartDate = StartDate,
                EndDate = EndDate,
                IsDone = false,
                UserId = userId
            };

            await _eventRepository.AddEventAsync(newEvent);

            var userName = User.Identity?.Name ?? "";

            var eventDto = new
            {
                id = newEvent.Id,
                title = newEvent.Title,
                description = newEvent.Description,
                startDate = newEvent.StartDate.ToString("yyyy-MM-dd HH:mm"),
                endDate = newEvent.EndDate.ToString("yyyy-MM-dd HH:mm"),
                isDone = newEvent.IsDone,
                userName,
                canEdit = true,
                canDelete = true
            };

            if (!User.IsInRole("Admin"))
            {
                await _hubContext.Clients.All.SendAsync("EventAdded", eventDto);
            }

            TempData["ToastMessage"] = "Dodano nowe wydarzenie!";
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleDoneAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var ev = await _eventRepository.GetEventByIdAsync(id, userId, isAdmin);
            if (ev == null)
                return NotFound();

            ev.IsDone = !ev.IsDone;
            await _eventRepository.UpdateEventAsync(ev);

            // Pobierz zadania powiązane z wydarzeniem
            var tasks = await _taskRepository.GetTasksByEventIdAsync(ev.Id);

            // Wysyłamy BlockTaskEdit do użytkowników, którzy edytują powiązane zadania
            foreach (var task in tasks)
            {
                if (!string.IsNullOrEmpty(task.UserId))
                {
                    if (ev.IsDone)
                    {
                        // Jeśli event wykonany - blokuj edycję zadania
                        await _hubContext.Clients.All
                            .SendAsync("BlockTaskEdit", task.Id);
                    }
                    else
                    {
                        // Jeśli event niewykonany - odblokuj edycję zadania
                        await _hubContext.Clients.All
                            .SendAsync("UnblockTaskEdit", task.Id);
                    }
                }
            }

            // DTO dla admina
            var eventAdminDto = new
            {
                ev.Id,
                ev.Title,
                ev.Description,
                StartDate = ev.StartDate.ToString("yyyy-MM-dd HH:mm"),
                EndDate = ev.EndDate.ToString("yyyy-MM-dd HH:mm"),
                ev.IsDone,
                UserName = ev.User?.UserName ?? "",
                CanEdit = true,
                CanDelete = true
            };

            // DTO dla użytkownika
            var eventUserDto = new
            {
                ev.Id,
                ev.Title,
                ev.Description,
                StartDate = ev.StartDate.ToString("yyyy-MM-dd HH:mm"),
                EndDate = ev.EndDate.ToString("yyyy-MM-dd HH:mm"),
                ev.IsDone,
                CanEdit = true,
                CanDelete = true
            };

            // DTO zadań dla admina
            var taskAdminDtos = tasks.Select(task => new
            {
                task.Id,
                task.Title,
                task.Description,
                StartDate = task.StartDate.ToString("yyyy-MM-dd HH:mm"),
                EndDate = task.EndDate.ToString("yyyy-MM-dd HH:mm"),
                task.IsDone,
                UserName = task.User?.UserName ?? "",
                CanEdit = !(ev.IsDone),
                CanDelete = true,
                ScrumEventDone = ev.IsDone
            }).ToList();

            // DTO zadań dla użytkownika
            var taskUserDtos = tasks.Select(task => new
            {
                task.Id,
                task.Title,
                task.Description,
                StartDate = task.StartDate.ToString("yyyy-MM-dd HH:mm"),
                EndDate = task.EndDate.ToString("yyyy-MM-dd HH:mm"),
                task.IsDone,
                CanEdit = !(ev.IsDone),
                CanDelete = true,
                ScrumEventDone = ev.IsDone
            }).ToList();

            var adminIds = await GetAdminUserIdsAsync();
            var userIds = await GetNonAdminUserIdsAsync(adminIds);

            if (adminIds.Count > 0)
            {
                await _hubContext.Clients.Users(adminIds).SendAsync("EventUpdated", eventAdminDto);
                await _hubContext.Clients.Users(adminIds).SendAsync("EventUpdatesTask", taskAdminDtos);
            }

            if (userIds.Count > 0)
            {
                await _hubContext.Clients.Users(userIds).SendAsync("EventUpdated", eventUserDto);
                await _hubContext.Clients.Users(userIds).SendAsync("EventUpdatesTask", taskUserDtos);
            }

            TempData["ToastMessage"] = "Status wydarzenia został zmieniony";
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var ev = await _eventRepository.GetEventByIdAsync(id, userId, isAdmin);
            if (ev == null)
                return NotFound();

            await _eventRepository.DeleteEventAsync(ev);

            // Powiadom wszystkich jeśli event usuwany jest nie przez konkretnego admina
            if (ev.UserId != "fe2c4ac1-87bd-4fef-9f91-954547d7d4f1")
            {
                await _hubContext.Clients.All.SendAsync("EventDeleted", ev.Id);
            }

            TempData["ToastMessage"] = "Wydarzenie zostało usunięte";
            TempData["ToastType"] = "danger";

            return RedirectToPage();
        }

        private async Task<List<string>> GetAdminUserIdsAsync()
        {
            const string adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c"; // zastąp właściwym ID roli
            return await _userRoleRepository.GetUserIdsInRoleAsync(adminRoleId);
        }

        private async Task<List<string>> GetNonAdminUserIdsAsync(List<string> adminIds)
        {
            return await _userRoleRepository.GetUserIdsNotInRolesAsync(adminIds);
        }

    }
}
