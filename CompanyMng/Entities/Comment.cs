namespace CompanyMng.Entities
{
    public class Comment
    {
        public int Id { get; set; }

        public int ProjectTaskId { get; set; }

        public int UserId { get; set; }

        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ProjectTask ProjectTask { get; set; } = null!;

        public AppUser User { get; set; } = null!;
    }
}
