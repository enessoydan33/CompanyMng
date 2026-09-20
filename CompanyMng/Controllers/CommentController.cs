using CompanyMng.Data;
using CompanyMng.Dto.CommentDto;
using CompanyMng.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CompanyMng.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentController:ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IValidator<CreateCommentDto> _validator;
        public CommentController(AppDbContext context, IValidator<CreateCommentDto> validator)
        {
            _context = context;
            _validator = validator;
            
        }


        [HttpPost("{taskId}")]
        public async Task<IActionResult> CreateComment(int taskId, CreateCommentDto dto)
        {
            var validationResult = await _validator.ValidateAsync(dto);

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
                return Unauthorized();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            // Task gerçekten giriş yapan kullanıcının şirketine mi ait?
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

            var comment = new Comment
            {
                ProjectTaskId = taskId,
                UserId = userId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                comment.Id,
                comment.ProjectTaskId,
                comment.UserId,
                comment.Content,
                comment.CreatedAt
            });
        }


        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetAllComment(int taskId)
        {
            var claimCmpId = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(claimCmpId, out var companyId))
                return Unauthorized();

            // Task bu şirkete ait mi?
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

            var comment =await _context.Comments.Where(x => x.ProjectTaskId == taskId).OrderByDescending(x => x.CreatedAt).Select(x => new
            {
                x.UserId,
                x.Content,
                x.CreatedAt,
                x.Id,
               UserName = x.User.Name + " " + x.User.Surname
            }).ToListAsync();


            return Ok(comment);
        }


        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteTask(int commentId)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId))
                return Unauthorized();

            var claimUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claimUserId, out var userId))
                return Unauthorized();



            var comment = await _context.Comments
                .FirstOrDefaultAsync(x =>
                    x.Id == commentId &&
                    x.ProjectTask.Project.CompanyId == companyId);

            if (comment == null)
                return NotFound(new { message = "Yorum bulunamadı" });

            var isAdmin = User.IsInRole("Admin");

            if (comment.UserId != userId && !isAdmin)
            {
                return Forbid();
            }

            _context.Comments.Remove(comment);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Yorum başarıyla silindi."
            });

        }

        }
}
