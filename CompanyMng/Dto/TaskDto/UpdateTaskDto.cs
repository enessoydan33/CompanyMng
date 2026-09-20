using CompanyMng.Enum;

namespace CompanyMng.Dto.TaskDto
{
    public class UpdateTaskDto
    {
        public string? Description { get; set; } 
        public string Title { get; set; } = null!;
        public DateTime? DueDate { get; set; }
        public int? AssignedUserId { get; set; }
        public TaskPriority Priority { get; set; }


    }
}
