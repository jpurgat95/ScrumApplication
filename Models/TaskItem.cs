namespace ScrumApplication.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = "ToDo"; // ToDo, InProgress, Done
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}
