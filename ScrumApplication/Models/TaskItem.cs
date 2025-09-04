using Microsoft.AspNetCore.Identity;

namespace ScrumApplication.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime EndDate { get; set; } = DateTime.Now.AddHours(1);

        public bool IsDone { get; set; } = false;
        public string UserId { get; set; }
        public IdentityUser User { get; set; }
        public int ScrumEventId { get; set; }
        public ScrumEvent? ScrumEvent { get; set; }
    }
}
