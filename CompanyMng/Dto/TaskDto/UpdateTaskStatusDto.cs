using CompanyMng.Enum;
using TaskStatusEnum = CompanyMng.Enum.TaskStatus;
namespace CompanyMng.Dto.TaskDto
{
    public class UpdateTaskStatusDto
    {
        public TaskStatusEnum Status { get; set; }
    }
}
