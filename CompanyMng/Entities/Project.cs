namespace CompanyMng.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int CreatedByUserId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        
        public string? Description { get; set; }
        public Company Company { get; set; } = null!;   
        public AppUser CreatedByUser { get; set; } = null!;
        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public ICollection<ProjectMember> Members{ get; set; } = new List<ProjectMember>();
    }
}
