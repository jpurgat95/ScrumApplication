using ScrumApplication.Models;

namespace ScrumApplication.Data;

public static class FakeDb
{
    private static List<TaskItem> _tasks = new List<TaskItem>
        {
            new TaskItem { Id = 1, Title = "Przygotować projekt rekrutacyjny", Description = "Utworzyć aplikację w ASP.NET Core", IsDone = false },
            new TaskItem { Id = 2, Title = "Nauczyć się Azure", Description = "Poznać podstawy chmury Microsoft Azure", IsDone = false }
        };

    public static List<TaskItem> GetTasks() => _tasks;

    public static void AddTask(TaskItem task)
    {
        task.Id = _tasks.Any() ? _tasks.Max(t => t.Id) + 1 : 1;
        _tasks.Add(task);
    }
}
