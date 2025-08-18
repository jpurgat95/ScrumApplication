namespace ScrumApplication.Models
{
    public class ScrumEvent
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime EndDate { get; set; } = DateTime.Now.AddHours(1);

        public bool IsDone { get; set; } = false;
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
