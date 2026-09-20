using CompanyMng.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace CompanyMng.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<AppUser> _userManager;

        public JwtTokenService(IConfiguration configuration, UserManager<AppUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<string> CreateToken(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),

                new Claim(JwtRegisteredClaimNames.Email,user.Email!),
                new Claim("CompanyId", user.CompanyId.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creditals = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims :claims,
                 expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(
                        _configuration["Jwt:AccessTokenMinutes"]!
                    )),
                 signingCredentials: creditals
                );

            return new JwtSecurityTokenHandler()
               .WriteToken(token);
        }



        public string CreateRefreshToken()
        {
            var randomBytes = new byte[64];

            RandomNumberGenerator.Fill(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }

        public string HashRefreshToken(string refreshToken)
        {
            var bytes = Encoding.UTF8.GetBytes(refreshToken);

            var hash = SHA256.HashData(bytes);

            return Convert.ToBase64String(hash);
        }


    }
}
