using ScrumApplication.Models;

namespace ScrumApplication.Data;

public static class FakeDb
{
    private static readonly List<TaskItem> tasks = new();
    private static int nextId = 1;

    public static List<TaskItem> GetTasks() => tasks;
    public static TaskItem? GetTaskById(int id) => tasks.FirstOrDefault(t => t.Id == id);
    public static void AddTask(TaskItem task) { task.Id = nextId++; task.CreatedAt = DateTime.Now; tasks.Add(task); }
    public static void UpdateTask(TaskItem updated) { var e = tasks.FirstOrDefault(t => t.Id == updated.Id); if (e != null) { e.Title = updated.Title; e.Description = updated.Description; e.IsDone = updated.IsDone; } }
    public static void RemoveTask(int id) { var e = tasks.FirstOrDefault(t => t.Id == id); if (e != null) tasks.Remove(e); }

}
