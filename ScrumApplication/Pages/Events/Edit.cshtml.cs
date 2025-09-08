using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using ScrumApplication.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScrumApplication.Pages.Events
{
    [Authorize]
    public class EditEventModel : PageModel
    {
        private readonly IEventRepository _eventRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IHubContext<UpdatesHub> _hubContext;

        public EditEventModel(IEventRepository eventRepository, IUserRoleRepository userRoleRepository, IHubContext<UpdatesHub> hubContext)
        {
            _eventRepository = eventRepository;
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
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                var ev = await _eventRepository.GetEventByIdAsync(id, userId, isAdmin);

                if (ev == null)
                    return NotFound();

                Id = ev.Id;
                Title = ev.Title;
                Description = ev.Description;
                StartDate = ev.StartDate;
                EndDate = ev.EndDate;

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Wystąpił błąd podczas próby wczytania strony edycji wydarzenia: {ex.Message}");
                return Page();
            }
        }
        public async Task<IActionResult> OnPostAsync()
        {
            try
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
                var isAdmin = User.IsInRole("Admin");

                var ev = await _eventRepository.GetEventByIdAsync(Id, userId, isAdmin);

                if (ev == null)
                    return NotFound();

                ev.Title = Title!;
                ev.Description = Description!;
                ev.StartDate = StartDate;
                ev.EndDate = EndDate;

                await _eventRepository.UpdateEventAsync(ev);

                // Przygotowanie DTO do wysyłki przez SignalR
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

                var adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c"; // id roli admina
                var adminIds = await _userRoleRepository.GetUserIdsInRoleAsync(adminRoleId);
                var userToSendId = ev.UserId;

                if (isAdmin)
                {
                    // Admin wysyła powiadomienie do konkretnego usera (np. eventUserDto)
                    await _hubContext.Clients.User(userToSendId).SendAsync("EventUpdated", eventUserDto);
                }
                else
                {
                    // Zwykły user wysyła powiadomienie do adminów
                    if (adminIds.Count > 0)
                    {
                        await _hubContext.Clients.Users(adminIds).SendAsync("EventUpdated", eventAdminDto);
                    }
                }

                TempData["ToastMessage"] = "Wydarzenie zostało zaktualizowane";
                TempData["ToastType"] = "success";

                return RedirectToPage("/Events/Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Wystąpił błąd podczas próby edycji wydarzenia: {ex.Message}");
                return Page();
            }
        }
    }
}
