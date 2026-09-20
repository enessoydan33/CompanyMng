using CompanyMng.Dto.AuthDto;
using FluentValidation;
using System.Data;

namespace CompanyMng.Validators.AuthDtoValidators
{
    public class RegisterDtoValidator: AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Name)
                 .NotEmpty().WithMessage("Ad alanı boş bırakılamaz.")
                 .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir.");

            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.")
                .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı boş bırakılamaz.")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre alanı boş bırakılamaz.")
                .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
            RuleFor(x => x.CompanyName)
                 .NotEmpty().WithMessage("Şirket adı boş bırakılamaz.")
                .MaximumLength(100).WithMessage("Şirket adı en fazla 100 karakter olabilir.");

        }
    }
}
