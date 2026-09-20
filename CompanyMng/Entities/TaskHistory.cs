using TaskStatusEnum = CompanyMng.Enum.TaskStatus;

namespace CompanyMng.Entities
{
    public class TaskHistory
    {
        public int Id { get; set; }
        public int ProjectTaskId { get; set; }
        public int UserId { get; set; }
        public TaskStatusEnum OldStatus { get; set; }
        public TaskStatusEnum NewStatus { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation

        public ProjectTask ProjectTask { get; set; } = null!;

        public AppUser User { get; set; } = null!;
    }
}
