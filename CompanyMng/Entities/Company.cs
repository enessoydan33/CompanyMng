namespace CompanyMng.Entities
{
    public class Company
    {
        public  int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();

        public ICollection<Project> Projects { get; set; } = new List<Project>();



    }
}
