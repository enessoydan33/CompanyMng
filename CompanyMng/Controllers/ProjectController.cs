using CompanyMng.Data;
using CompanyMng.Dto.ProjectDto;
using CompanyMng.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CompanyMng.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IValidator<CreateProjectDto> _createProjectValidator;
        private readonly IValidator<UpdateProjectDto> _updateProjectValidator;


        public ProjectController(AppDbContext context, IValidator<CreateProjectDto> createProjectDto,IValidator<UpdateProjectDto> updateProjectValidator)
        {
            _context = context;
            _createProjectValidator = createProjectDto;
            _updateProjectValidator = updateProjectValidator;
        }



        [Authorize(Roles = "Admin")]
        [HttpPost("create")]
        public async Task<IActionResult> Create(CreateProjectDto dto)
        {
            var validatorResult = await _createProjectValidator.ValidateAsync(dto);

            if (!validatorResult.IsValid)
            {
                return BadRequest(validatorResult.Errors.Select(x => new
                {
                    field = x.PropertyName,
                    error = x.ErrorMessage
                }));

            }

            var CompanyIdClaim = User.FindFirst("CompanyId")?.Value;
            var UserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;



            if (!int.TryParse(CompanyIdClaim, out var companyId) ||
               !int.TryParse(UserIdClaim, out var userId))
            {
                return Unauthorized();
            }



            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                CompanyId = companyId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                project.Id,
                project.Name,
                project.Description,
                project.CreatedAt
            });

        }


        [HttpGet]      
        public async Task<IActionResult> GetAll()
        {
            var claimCmpId = User.FindFirst("CompanyId")?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimCmpId, out var companyId) ||
                !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");

            var query = _context.Projects
                .Where(x => x.CompanyId == companyId);

            if (!isAdmin)
            {
                query = query.Where(x =>
                    x.Members.Any(m => m.UserId == userId));
            }

            var projects = await query
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.CreatedByUserId,
                    x.CreatedAt,
                    x.Description
                })
                .ToListAsync();

            return Ok(new
            {
                project = projects
            });

        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var claimCmpId = User.FindFirst("CompanyId")?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimCmpId, out var companyId) ||
                !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");

            var query = _context.Projects
                .Where(x =>
                    x.Id == id &&
                    x.CompanyId == companyId);

            if (!isAdmin)
            {
                query = query.Where(x =>
                    x.Members.Any(m => m.UserId == userId));
            }

            var project = await query
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Description,
                    x.CreatedByUserId,
                    x.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (project == null)
            {
                return NotFound(new
                {
                    message = "Proje bulunamadı."
                });
            }

            return Ok(project);


        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProject(int id,UpdateProjectDto dto)
        {
            var resultValidator = await _updateProjectValidator.ValidateAsync(dto);

            if (!resultValidator.IsValid)
            {
                return BadRequest(resultValidator.Errors.Select(x => new
                {
                    field = x.PropertyName,
                    error = x.ErrorMessage
                }));


            }

            var claimCmp = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(claimCmp, out var companyId))
            {
                return Unauthorized();
            }

            var project = await _context.Projects.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id);

            if (project == null)
            {
                return NotFound(new
                {
                    message = "Proje bulunamadı."
                });
            }
            project.Name = dto.Name;
            project.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                project.Id,
                project.Name,
                project.Description,
                project.CreatedByUserId,
                project.CreatedAt
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var claimCmpId = User.FindFirst("CompanyId")?.Value;

            if(!int.TryParse(claimCmpId,out var companyId))
            {
                return Unauthorized();
            }

            var project =await _context.Projects.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id);
            if(project==null)
            {
                return NotFound(new
                {
                    message="Proje Bulunamadı"
                });
            }

             _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
