namespace ScrumApplication.Models;

public class ScrumEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // np. "Daily Scrum", "Sprint Planning"
    public DateTime ScheduledAt { get; set; }
    public string Description { get; set; } = string.Empty;
}
