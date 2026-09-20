using Microsoft.AspNetCore.Identity;
namespace CompanyMng.Entities
{
    public class AppUser:IdentityUser<int>
    {
        public int CompanyId { get; set; }

        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public Company Company { get; set; } = null!;
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<Project> CreatedProjects { get; set; }= new List<Project>();
                         

    }
}
