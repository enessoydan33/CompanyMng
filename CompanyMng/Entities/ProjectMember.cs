namespace CompanyMng.Entities
{
    public class ProjectMember
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        // Navigation
        public Project Project { get; set; } = null!;

        public AppUser User { get; set; } = null!;

    }
}
