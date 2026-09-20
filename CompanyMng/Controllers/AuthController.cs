using CompanyMng.Data;
using CompanyMng.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CompanyMng.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CompanyMng.Dto.AuthDto;
using System.ComponentModel.Design;
namespace CompanyMng.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly AppDbContext _context;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IValidator<CreateUserDto> _createUserValidator;
        public AuthController(UserManager<AppUser> userManager, IValidator<RegisterDto> registerValidator,AppDbContext context, IValidator<LoginDto> loginValidator,JwtTokenService jwtTokenService,IValidator<CreateUserDto> createUserValidator)
        {
            _userManager = userManager;
            _registerValidator = registerValidator;
            _context = context;
            _loginValidator = loginValidator;
            _jwtTokenService = jwtTokenService;
            _createUserValidator = createUserValidator;
        }



        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var validationResult = await _registerValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(x => new
                {
                    field = x.PropertyName,
                    error = x.ErrorMessage
                }));
            }

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Bu email adresi zaten kullanılıyor."
                });
            }

            await using var transaction =
       await _context.Database.BeginTransactionAsync();
            try
            {
                var company = new Company
                {
                    Name = dto.CompanyName,
                    CreatedAt = DateTime.UtcNow
                };
                    _context.Companies.Add(company);
                     await _context.SaveChangesAsync();

                var user = new AppUser
                {
                    Name = dto.Name,
                    Surname = dto.Surname,
                    Email = dto.Email,
                    UserName = dto.Email,
                    CreatedAt = DateTime.UtcNow,
                    CompanyId = company.Id
                };


              

                var result = await _userManager.CreateAsync(user, dto.Password);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(result.Errors.Select(x => new
                    {
                        code = x.Code,
                        error = x.Description
                    }));
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "Admin");

                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(roleResult.Errors);
                }

               

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Kullanıcı başarıyla oluşturuldu."
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var validationResult = await _loginValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(x => new
                {
                    field = x.PropertyName,
                    error = x.ErrorMessage
                }));
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Email veya şifre hatalı."
                });
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);

            if (!passwordValid)
            {
                return Unauthorized(new
                {
                    message = "Email veya şifre hatalı."
                });
            }

            var oldRefreshTokens = await _context.RefreshTokens
                                   .Where(x =>
                                           x.UserId == user.Id &&
                                           !x.IsRevoked)
                                           .ToListAsync();

            foreach (var oldToken in oldRefreshTokens)
            {
                oldToken.IsRevoked = true;
            }




            var token = await _jwtTokenService.CreateToken(user);
            var refreshToken = _jwtTokenService.CreateRefreshToken();
            var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshToken);

            var refreshTokenEnt = new RefreshToken
            {
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
           
            _context.RefreshTokens.Add(refreshTokenEnt);

           


            await _context.SaveChangesAsync();

            return Ok(new
            {
                accessToken = token,
                refreshToken = refreshToken
            });

            
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> refresh(RefreshTokenDto dto)
        {


            var refreshTokenHash =_jwtTokenService.HashRefreshToken(dto.RefreshToken);
            var storedToken = _context.RefreshTokens.Include(x => x.User).FirstOrDefault(x => x.TokenHash == refreshTokenHash && !x.IsRevoked);

            if(storedToken==null)
            {
                return Unauthorized(new
                {
                    message = "Token Geçersiz"
                });
            }

            if(storedToken.ExpiresAt<=DateTime.UtcNow)
            {
                return Unauthorized(new
                {
                    message = "Token günü dolmuş"
                });
            }

            var newtoken =await _jwtTokenService.CreateToken(storedToken.User);
            var newRefrTkn = _jwtTokenService.CreateRefreshToken();
            var newHshTkn = _jwtTokenService.HashRefreshToken(newRefrTkn);
            storedToken.IsRevoked = true;

            var TokenEnt = new RefreshToken
            {
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                TokenHash=newHshTkn,
                UserId=storedToken.UserId
            };

            _context.RefreshTokens.Add(TokenEnt);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                accessToken = newtoken,
                refreshToken = newRefrTkn
            });

        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenDto dto)
        {
            var refreshtokenH = _jwtTokenService.HashRefreshToken(dto.RefreshToken);

            var storedToken = _context.RefreshTokens.FirstOrDefault(x => x.TokenHash == refreshtokenH && !x.IsRevoked);


            if (storedToken == null)
            {
                return Unauthorized(new
                {
                    message = "Refresh token geçersiz."
                });
            }

            storedToken.IsRevoked = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Çıkış Başarılı"
            });


        }


        [Authorize(Roles = "Admin")]
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            var resultValidator =await _createUserValidator.ValidateAsync(dto);

            if(!resultValidator.IsValid)
            {
                return BadRequest(resultValidator.Errors.Select(x => new
                {
                    field = x.PropertyName,
                    error = x.ErrorMessage
                }));
            }

            var claimCmpId = User.FindFirst("CompanyId")?.Value;

            if(!int.TryParse(claimCmpId,out var cmpId))
            {
                return Unauthorized();
            }

            var existingUser =
             await _userManager.FindByEmailAsync(dto.Email);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Bu email adresi zaten kullanılıyor."
                });
            }

            var newUser = new AppUser
            {
                Name = dto.Name,
                Surname = dto.Surname,
                Email = dto.Email,
                UserName = dto.Email,
                CompanyId = cmpId,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser, dto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(x => new
                {
                    error = x.Description
                }));
            }

            var roleResult =
                await _userManager.AddToRoleAsync(newUser, "User");

            if (!roleResult.Succeeded)
            {
                return BadRequest(roleResult.Errors.Select(x => new
                {
                    error = x.Description
                }));
            }
                
            return Ok(new
            {
                newUser.Id,
                newUser.Name,
                newUser.Surname,
                newUser.Email,
                newUser.CompanyId
            });

        }

        
    }

}
