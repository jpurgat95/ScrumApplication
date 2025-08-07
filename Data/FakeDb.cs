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

        // Jeśli nie ustawiono daty, przypisz domyślne
        if (task.StartDate == default)
            task.StartDate = DateTime.Now;
        if (task.EndDate == default)
            task.EndDate = task.StartDate.AddHours(1);

        tasks.Add(task);
    }

    public static void UpdateTask(TaskItem updated)
    {
        var t = tasks.FirstOrDefault(t => t.Id == updated.Id);
        if (t != null)
        {
            t.Title = updated.Title;
            t.Description = updated.Description;
            t.StartDate = updated.StartDate;
            t.EndDate = updated.EndDate;
            t.IsDone = updated.IsDone;
        }
    }

    public static void RemoveTask(int id)
    {
        var t = tasks.FirstOrDefault(t => t.Id == id);
        if (t != null)
            tasks.Remove(t);
    }

    // Wydarzenia
    public static List<ScrumEvent> GetEvents() => events;

    public static ScrumEvent? GetEventById(int id) => events.FirstOrDefault(ev => ev.Id == id);

    public static void AddEvent(ScrumEvent ev)
    {
        ev.Id = nextEventId++;

        // Jeśli nie ustawiono dat, przypisz domyślne
        if (ev.StartDate == default)
            ev.StartDate = DateTime.Now;
        if (ev.EndDate == default)
            ev.EndDate = ev.StartDate.AddHours(1);

        events.Add(ev);
    }

    public static void UpdateEvent(ScrumEvent updated)
    {
        var e = events.FirstOrDefault(ev => ev.Id == updated.Id);
        if (e != null)
        {
            e.Title = updated.Title;
            e.Description = updated.Description;
            e.StartDate = updated.StartDate;
            e.EndDate = updated.EndDate;
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
