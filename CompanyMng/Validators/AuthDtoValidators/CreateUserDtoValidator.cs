using CompanyMng.Dto.AuthDto;
using FluentValidation;

namespace CompanyMng.Validators.AuthDtoValidators
{
    public class CreateUserDtoValidator:AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.Name)
                  .NotEmpty()
                  .WithMessage("İsim boş olamaz.")
                  .MaximumLength(50)
                  .WithMessage("İsim en fazla 50 karakter olabilir.");

            RuleFor(x => x.Surname)
                .NotEmpty()
                .WithMessage("Soyisim boş olamaz.")
                .MaximumLength(50)
                .WithMessage("Soyisim en fazla 50 karakter olabilir.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email boş olamaz.")
                .EmailAddress()
                .WithMessage("Geçerli bir email adresi giriniz.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Şifre boş olamaz.")
                .MinimumLength(8)
                .WithMessage("Şifre en az 8 karakter olmalıdır.");
        }
    }
}
