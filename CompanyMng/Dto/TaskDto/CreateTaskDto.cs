using CompanyMng.Enum;

namespace CompanyMng.Dto.TaskDto
{
    public class CreateTaskDto
    {

        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public int? AssignedUserId { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;


    }
}
