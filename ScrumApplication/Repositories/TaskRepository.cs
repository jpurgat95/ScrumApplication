using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using ScrumApplication.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TaskRepository : ITaskRepository
{
    private readonly ScrumDbContext _context;

    public TaskRepository(ScrumDbContext context)
    {
        _context = context;
    }

    public async Task<List<TaskItem>> GetTasksAsync(string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _context.Tasks
                .Include(t => t.ScrumEvent)
                .Include(t => t.User)
                .OrderBy(t => t.StartDate)
                .ToListAsync();
        }
        else
        {
            return await _context.Tasks
                .Include(t => t.ScrumEvent)
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.StartDate)
                .ToListAsync();
        }
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int id, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _context.Tasks
                .Include(t => t.ScrumEvent)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        else
        {
            return await _context.Tasks
                .Include(t => t.ScrumEvent)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        }
    }

    public async Task AddTaskAsync(TaskItem task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(TaskItem task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(TaskItem task)
    {
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
    }
    public async Task<List<TaskItem>> GetTasksByEventIdAsync(int eventId)
    {
        return await _context.Tasks
            .Include(t => t.User)           // Załaduj użytkownika powiązanego z zadaniem
            .Include(t => t.ScrumEvent)     // Załaduj powiązane wydarzenie
            .Where(t => t.ScrumEventId == eventId)
            .OrderBy(t => t.StartDate)
            .ToListAsync();
    }
}
