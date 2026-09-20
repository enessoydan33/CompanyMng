using CompanyMng.Data;
using CompanyMng.Dto.TaskDto;
using CompanyMng.Entities;
using CompanyMng.Enum;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using TaskStatusEnum = CompanyMng.Enum.TaskStatus;

namespace CompanyMng.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IValidator<CreateTaskDto> _createTaskValidator;
        private readonly IValidator<UpdateTaskDto> _updateTaskValidator;
        private readonly IValidator<UpdateTaskStatusDto> _updateTaskStatusValidator;

        public TaskController(
            AppDbContext context,
            IValidator<CreateTaskDto> createTaskValidator,
            IValidator<UpdateTaskDto> updateTaskValidator,IValidator<UpdateTaskStatusDto> updateTaskStatusValidator)
        {
            _context = context;
            _createTaskValidator = createTaskValidator;
            _updateTaskValidator = updateTaskValidator;
            _updateTaskStatusValidator = updateTaskStatusValidator;
        }

        [HttpPost("{projectId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTask(
    int projectId,
    CreateTaskDto dto)
        {
            var validationResult =
                await _createTaskValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(x => new
                {
                    field = x.PropertyName,
                    error = x.ErrorMessage
                }));
            }

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            // Project gerçekten bu şirkete mi ait?
            var project = await _context.Projects
                .FirstOrDefaultAsync(x =>
                    x.Id == projectId &&
                    x.CompanyId == companyId);

            if (project == null)
            {
                return NotFound(new
                {
                    message = "Proje bulunamadı."
                });
            }

            // Task bir kullanıcıya atanacaksa,
            // o kullanıcı bu projenin üyesi olmak zorunda.
            if (dto.AssignedUserId.HasValue)
            {
                var isMember = await _context.ProjectMembers
                    .AnyAsync(x =>
                        x.ProjectId == projectId &&
                        x.UserId == dto.AssignedUserId.Value);

                if (!isMember)
                {
                    return BadRequest(new
                    {
                        message = "Task atanacak kullanıcı bu projenin üyesi değil."
                    });
                }
            }

            var task = new ProjectTask
            {
                ProjectId = projectId,
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                AssignedUserId = dto.AssignedUserId,
                Priority = dto.Priority,
                Status = Enum.TaskStatus.Todo, // namespace'ine göre düzelt
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                task.Id,
                task.ProjectId,
                task.Title,
                task.Description,
                task.AssignedUserId,
                task.Priority,
                task.Status,
                task.DueDate,
                task.CreatedAt
            });
        }


        [HttpGet("project/{projectId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllTask(int projectId,[FromQuery] TaskQueryDto dto)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            // Önce proje bu şirkete ait mi?
            var projectExists = await _context.Projects
                .AnyAsync(x =>
                    x.Id == projectId &&
                    x.CompanyId == companyId);

            if (!projectExists)
            {
                return NotFound(new
                {
                    message = "Proje bulunamadı."
                });
            }

            if (dto.PageNumber < 1)
                dto.PageNumber = 1;

            if (dto.PageSize < 1)
                dto.PageSize = 10;

            if (dto.PageSize > 100)
                dto.PageSize = 100;



            var query = _context.ProjectTasks
       .Where(x => x.ProjectId == projectId);

            query = ApplyFilters(query, dto);


            var totalCount =await query.CountAsync();

            var tasks=await query.OrderByDescending(x=>x.CreatedAt).Skip((dto.PageNumber-1)*dto.PageSize).Take(dto.PageSize).Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.Status,
                x.Priority,
                x.DueDate,
                x.AssignedUserId,

                AssignedUserName = x.AssignedUser != null
            ? x.AssignedUser.Name + " " + x.AssignedUser.Surname
            : null,

                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync();

            return Ok(new
            {
                dto.PageNumber,
                dto.PageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(
                totalCount / (double)dto.PageSize),

                data = tasks
            });
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdTask(int id)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            var task = await _context.ProjectTasks.Where(x => x.Id == id && x.Project.CompanyId == companyId).Select(x => new
            {
                x.Id,
                x.ProjectId,
                x.Title,
                x.Description,
                x.Status,
                x.Priority,
                x.DueDate,
                x.AssignedUserId,

                AssignedUserName = x.AssignedUser != null
                ? x.AssignedUser.Name + " " + x.AssignedUser.Surname
                : null,

                x.CreatedAt,
                x.UpdatedAt
            })
        .FirstOrDefaultAsync();

            if (task == null)
            {
                return NotFound(new
                {
                    message = "Task bulunamadı."
                });
            }

            return Ok(task);


        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{taskId}")]
        public async Task<IActionResult> UpdateTask(int taskId,UpdateTaskDto dto)
        {
            var resultVal =await _updateTaskValidator.ValidateAsync(dto);
            if(!resultVal.IsValid)
            {

                return BadRequest(resultVal.Errors.Select(x => new
                {
                    field = x.PropertyName,
                    error = x.ErrorMessage
                }));
            }

            
            var claimCmpId = User.FindFirst("CompanyId")?.Value;
             
            if(!int.TryParse(claimCmpId,out var companyId))
            {
                return Unauthorized();
            }

            var task =await _context.ProjectTasks.FirstOrDefaultAsync(x => x.Id == taskId && x.Project.CompanyId == companyId);

            if (task == null)
                return NotFound(new { message = "Task bulunamadı." });

            if(dto.AssignedUserId.HasValue)
            {
                var isMember = await _context.ProjectMembers.AnyAsync(x => x.UserId == dto.AssignedUserId.Value && x.ProjectId== task.ProjectId );

                if (!isMember)
                {
                    return BadRequest(new
                    {
                        message = "Task atanacak kullanıcı bu projenin üyesi değil."
                    });
                }
            }

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.DueDate = dto.DueDate;
            task.AssignedUserId = dto.AssignedUserId;
            task.Priority = dto.Priority;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                task.Id,
                task.ProjectId,
                task.Title,
                task.Description,
                task.AssignedUserId,
                task.Priority,
                task.Status,
                task.DueDate,
                task.UpdatedAt
            });

        }


        [HttpDelete("{taskId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(x =>
                    x.Id == taskId &&
                    x.Project.CompanyId == companyId);

            if (task == null)
            {
                return NotFound(new
                {
                    message = "Task bulunamadı."
                });
            }

            _context.ProjectTasks.Remove(task);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Task başarıyla silindi."
            });
        }

        [HttpPatch("{taskId}/status")]
      
        public async Task<IActionResult> UpdateTaskStatus(int taskId,UpdateTaskStatusDto dto)
        {
            
            
            
            var resultVal =await _updateTaskStatusValidator.ValidateAsync(dto);

            if(!resultVal.IsValid)
            {
                return BadRequest(resultVal.Errors.Select(x => new
                {
                    field = x.PropertyName,
                    error = x.ErrorMessage
                }));

            }

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId))
                return Unauthorized();

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var task = await _context.ProjectTasks
                .FirstOrDefaultAsync(x =>
                    x.Id == taskId &&
                    x.Project.CompanyId == companyId);

            var isAdmin = User.IsInRole("Admin");

            if (task == null)
                return NotFound(new { message = "Task bulunamadı." });
            
            
            if (!isAdmin && task.AssignedUserId != userId)
            {
                return Forbid();
            }

          

            var oldStatus = task.Status;

            if (oldStatus == dto.Status)
            {
                return BadRequest(new
                {
                    message = "Task zaten bu durumda."
                });
            }

            task.Status = dto.Status;
            task.UpdatedAt = DateTime.UtcNow;

            var history = new TaskHistory
            {
                ProjectTaskId = task.Id,
                UserId = userId,
                OldStatus = (TaskStatusEnum)oldStatus,
                NewStatus = (TaskStatusEnum)dto.Status,
                ChangedAt = DateTime.UtcNow
            };

            _context.TaskHistories.Add(history);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                task.Id,
                task.Status,
                task.UpdatedAt
            });


        }


        [HttpGet("{taskId}/history")]
        public async Task<IActionResult> GetTaskHistory(int taskId)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId))
                return Unauthorized();

            var taskExists = await _context.ProjectTasks
                .AnyAsync(x =>
                    x.Id == taskId &&
                    x.Project.CompanyId == companyId);

            if (!taskExists)
            {
                return NotFound(new
                {
                    message = "Task bulunamadı."
                });
            }

            var histories = await _context.TaskHistories
                .Where(x => x.ProjectTaskId == taskId)
                .OrderByDescending(x => x.ChangedAt)
                .Select(x => new
                {
                    x.Id,
                    x.OldStatus,
                    x.NewStatus,
                    x.ChangedAt,

                    ChangedBy = x.User.Name + " " + x.User.Surname
                })
                .ToListAsync();

            return Ok(histories);
        }

        [HttpPatch("{taskId}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignTask(int taskId,AssignTaskDto dto)
        {
            var claimCmp = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(claimCmp, out var cmpId))
                return Unauthorized();
               
            

            var task = await _context.ProjectTasks.FirstOrDefaultAsync(x => x.Id == taskId && x.Project.CompanyId == cmpId);

            if (task == null)
                return NotFound(new { message = "Task bulunamadı." });

            var isMember = await _context.ProjectMembers.Where(x => x.UserId == dto.UserId && x.ProjectId == task.ProjectId).AnyAsync();

            if (!isMember)
            {
                return BadRequest(new
                {
                    message = "Task atanacak kullanıcı bu projenin üyesi değil."
                });
            }

            task.AssignedUserId = dto.UserId;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                task.Id,
                task.AssignedUserId,
                task.UpdatedAt

            });

        }




        private IQueryable<ProjectTask> ApplyFilters(
                 IQueryable<ProjectTask> query,
                    TaskQueryDto dto)
        {
            if (dto.Status.HasValue)
                query = query.Where(x => x.Status == dto.Status.Value);

            if (dto.Priority.HasValue)
                query = query.Where(x => x.Priority == dto.Priority.Value);

            if (dto.AssignedUserId.HasValue)
                query = query.Where(x =>
                    x.AssignedUserId == dto.AssignedUserId.Value);

            if (!string.IsNullOrWhiteSpace(dto.Search))
                query = query.Where(x =>
                    x.Title.Contains(dto.Search));

            return query;
        }





    }
}
