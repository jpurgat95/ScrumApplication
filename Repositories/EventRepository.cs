using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class EventRepository : IEventRepository
{
    private readonly ScrumDbContext _context;

    public EventRepository(ScrumDbContext context)
    {
        _context = context;
    }

    public async Task<List<ScrumEvent>> GetEventsAsync(string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _context.Events
                .Include(e => e.User)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }
        else
        {
            return await _context.Events
                .Include(e => e.User)
                .Where(e => e.UserId == userId)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }
    }

    public async Task<ScrumEvent?> GetEventByIdAsync(int id, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _context.Events
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);
        }
        else
        {
            return await _context.Events
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        }
    }

    public async Task AddEventAsync(ScrumEvent newEvent)
    {
        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEventAsync(ScrumEvent updatedEvent)
    {
        _context.Events.Update(updatedEvent);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteEventAsync(ScrumEvent eventToDelete)
    {
        _context.Events.Remove(eventToDelete);
        await _context.SaveChangesAsync();
    }
}