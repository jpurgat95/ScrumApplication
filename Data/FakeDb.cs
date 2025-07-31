using ScrumApplication.Models;

namespace ScrumApplication.Data;

public static class FakeDb
{
    public static List<TaskItem> Tasks { get; set; } = new()
        {
            new TaskItem { Id = 1, Title = "Stworzenie projektu", Description = "Utworzenie szkieletu aplikacji", Status = "Done", AssignedTo = "Jan", DueDate = DateTime.Now.AddDays(1) },
            new TaskItem { Id = 2, Title = "Dodanie listy zadań", Description = "Wyświetlenie zadań na stronie", Status = "ToDo", AssignedTo = "Anna", DueDate = DateTime.Now.AddDays(3) }
        };

    public static List<ScrumEvent> Events { get; set; } = new()
        {
            new ScrumEvent { Id = 1, Name = "Daily Scrum", ScheduledAt = DateTime.Today.AddHours(9), Description = "Codzienne spotkanie zespołu" },
            new ScrumEvent { Id = 2, Name = "Sprint Planning", ScheduledAt = DateTime.Today.AddDays(1).AddHours(10), Description = "Planowanie sprintu" }
        };
}
