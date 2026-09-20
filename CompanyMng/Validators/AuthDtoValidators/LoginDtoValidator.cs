using CompanyMng.Dto.AuthDto;
using FluentValidation;

namespace CompanyMng.Validators.AuthDtoValidators
{
    public class LoginDtoValidator: AbstractValidator<LoginDto>
    {

        public LoginDtoValidator()
        {
               RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı boş bırakılamaz.")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.");

        RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre alanı boş bırakılamaz.")
                .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
        }

     
    }
}
