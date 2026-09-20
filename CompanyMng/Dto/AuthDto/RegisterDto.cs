using System.ComponentModel.DataAnnotations;

namespace CompanyMng.Dto.AuthDto
{
    public class RegisterDto
    {
        public string Name { get; set; } = null!;
       
        public string Surname { get; set; } = null!;
       
        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
    }
}
