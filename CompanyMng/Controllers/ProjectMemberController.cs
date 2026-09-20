using CompanyMng.Data;
using CompanyMng.Dto.ProjectMember;
using CompanyMng.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace CompanyMng.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectMemberController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProjectMemberController(AppDbContext context)
        {
            _context = context;
        }

         [HttpPost("{projectId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddMember(int projectId,AddProjectMemberDto dto)
          {
            var claimCmpId = User.FindFirst("CompanyId")?.Value;

            if(!int.TryParse(claimCmpId,out var cmpId))
            {
                return Unauthorized();
            }
            
            var project = await _context.Projects
             .FirstOrDefaultAsync(x =>
                 x.Id == projectId &&
                 x.CompanyId == cmpId);

            if (project == null)
            {
                return NotFound(new
                {
                    message = "Proje bulunamadı."
                });
            }


            var user = await _context.Users
              .FirstOrDefaultAsync(x =>
                  x.Id == dto.UserId &&
                  x.CompanyId == cmpId);

            if (user == null)
            {
                return BadRequest(new
                {
                    message = "Kullanıcı bu şirkete ait değil."
                });
            }



            var alreadyMember = await _context.ProjectMembers
               .AnyAsync(x =>
                   x.ProjectId == projectId &&
                   x.UserId == dto.UserId);

            if (alreadyMember)
            {
                return BadRequest(new
                {
                    message = "Kullanıcı zaten bu projenin üyesi."
                });
            }

            var projectMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = dto.UserId,
                JoinedAt = DateTime.UtcNow
            };

            _context.ProjectMembers.Add(projectMember);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                projectMember.Id,
                projectMember.ProjectId,
                projectMember.UserId,
                projectMember.JoinedAt
            });



        }


        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetMembers(int projectId)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            // Proje bu şirkete ait mi?
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

            var members = await _context.ProjectMembers
                .Where(x => x.ProjectId == projectId)
                .Select(x => new
                {
                    x.UserId,
                    x.User.Name,
                    x.User.Surname,
                    x.User.Email,
                    x.JoinedAt
                })
                .ToListAsync();

            return Ok(members);
        }


        [HttpDelete("{projectId}/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveMember(int projectId, int userId)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;

            if (!int.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized();
            }

            // Proje bu şirkete ait mi?
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

            // Kullanıcı bu projenin üyesi mi?
            var projectMember = await _context.ProjectMembers
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == projectId &&
                    x.UserId == userId);

            if (projectMember == null)
            {
                return NotFound(new
                {
                    message = "Kullanıcı bu projenin üyesi değil."
                });
            }

            _context.ProjectMembers.Remove(projectMember);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Kullanıcı projeden çıkarıldı."
            });
        }
    }
}
