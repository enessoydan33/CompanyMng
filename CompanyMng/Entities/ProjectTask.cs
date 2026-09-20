using CompanyMng.Enum;
using TaskStatus = CompanyMng.Enum.TaskStatus;

namespace CompanyMng.Entities
{
    public class ProjectTask
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? AssignedUserId { get; set; }

        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public TaskStatus Status { get; set; } = TaskStatus.Todo;

        // Navigation

        public Project Project { get; set; } = null!;

        public AppUser? AssignedUser { get; set; }

        public ICollection<Comment> Comments { get; set; }
            = new List<Comment>();

        public ICollection<TaskHistory> Histories { get; set; }
            = new List<TaskHistory>();


    }
}
