using ScrumApplication.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IEventRepository
{
    Task<List<ScrumEvent>> GetEventsAsync(string userId, bool isAdmin);
    Task<ScrumEvent?> GetEventByIdAsync(int id, string userId, bool isAdmin);
    Task AddEventAsync(ScrumEvent newEvent);
    Task UpdateEventAsync(ScrumEvent updatedEvent);
    Task DeleteEventAsync(ScrumEvent eventToDelete);
}