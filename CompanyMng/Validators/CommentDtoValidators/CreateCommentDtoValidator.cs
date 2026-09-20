using CompanyMng.Dto.CommentDto;
using FluentValidation;

namespace CompanyMng.Validators.CommentDtoValidators
{
    public class CreateCommentDtoValidator:AbstractValidator<CreateCommentDto>
    {
        public CreateCommentDtoValidator()
        {
            RuleFor(x => x.Content).MaximumLength(1000).WithMessage("1000 den fazla karakter olamaz").NotEmpty().WithMessage("Boş Olamaz");
        }
    }
}
