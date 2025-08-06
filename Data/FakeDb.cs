using ScrumApplication.Models;

namespace ScrumApplication.Data;

public static class FakeDb
{
    private static readonly List<TaskItem> tasks = new();
    private static readonly List<ScrumEvent> events = new();
    private static int nextTaskId = 1;
    private static int nextEventId = 1;

    // Zadania
    public static List<TaskItem> GetTasks() => tasks;

    public static TaskItem? GetTaskById(int id) => tasks.FirstOrDefault(t => t.Id == id);

    public static void AddTask(TaskItem task)
    {
        task.Id = nextTaskId++;
        task.Date = DateTime.Now;
        tasks.Add(task);
    }

    public static void UpdateTask(TaskItem updated)
    {
        var e = tasks.FirstOrDefault(t => t.Id == updated.Id);
        if (e != null)
        {
            e.Title = updated.Title;
            e.Description = updated.Description;
            e.Date = updated.Date;
            e.IsDone = updated.IsDone;
        }
    }

    public static void RemoveTask(int id)
    {
        var e = tasks.FirstOrDefault(t => t.Id == id);
        if (e != null)
            tasks.Remove(e);
    }

    // Wydarzenia
    public static List<ScrumEvent> GetEvents() => events;

    public static ScrumEvent? GetEventById(int id) => events.FirstOrDefault(ev => ev.Id == id);

    public static void AddEvent(ScrumEvent ev)
    {
        ev.Id = nextEventId++;
        if (ev.Date == default)
            ev.Date = DateTime.Now;
        events.Add(ev);
    }

    public static void UpdateEvent(ScrumEvent updated)
    {
        var e = events.FirstOrDefault(ev => ev.Id == updated.Id);
        if (e != null)
        {
            e.Title = updated.Title;
            e.Description = updated.Description;
            e.Date = updated.Date;
            e.IsDone = updated.IsDone;
        }
    }

    public static void RemoveEvent(int id)
    {
        var e = events.FirstOrDefault(ev => ev.Id == id);
        if (e != null)
            events.Remove(e);
    }
}
