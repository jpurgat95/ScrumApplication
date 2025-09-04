using ScrumApplication.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ITaskRepository
{
    Task<List<TaskItem>> GetTasksAsync(string userId, bool isAdmin);
    Task<TaskItem?> GetTaskByIdAsync(int id, string userId, bool isAdmin);
    Task AddTaskAsync(TaskItem task);
    Task UpdateTaskAsync(TaskItem task);
    Task DeleteTaskAsync(TaskItem task);
    Task<List<TaskItem>> GetTasksByEventIdAsync(int eventId);
}
