using CompanyMng.Enum;

namespace CompanyMng.Dto.TaskDto
{
    public class TaskQueryDto
    {
        
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 10;

            public int? AssignedUserId { get; set; }
            public Enum.TaskStatus? Status { get; set; }
            public TaskPriority? Priority { get; set; }
            public string? Search { get; set; }

            public string SortBy { get; set; } = "createdAt";
            public string SortOrder { get; set; } = "desc";
        
    }
}
