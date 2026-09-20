using Microsoft.EntityFrameworkCore;
using CompanyMng.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace CompanyMng.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.Entity<ProjectMember>()
                     .HasIndex(x => new { x.ProjectId, x.UserId })
                     .IsUnique();

            builder.Entity<Project>()
                    .HasOne(x => x.CreatedByUser)
                     .WithMany(x => x.CreatedProjects)
                     .HasForeignKey(x => x.CreatedByUserId)
                     .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<ProjectTask>()
            .HasOne(x => x.AssignedUser)
             .WithMany()
             .HasForeignKey(x => x.AssignedUserId)
             .OnDelete(DeleteBehavior.SetNull);

            
        }

    }
}
