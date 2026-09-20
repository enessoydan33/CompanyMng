using CompanyMng.Dto.ProjectDto;
using FluentValidation;

namespace CompanyMng.Validators.ProjectDtoValidators
{
    public class UpdateProjectDtoValidators : AbstractValidator<UpdateProjectDto>
    {
        public UpdateProjectDtoValidators()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Proje adı boş Olamaz")
             .MaximumLength(100).WithMessage("Proje adı maksimum 100 karakter olabilir");
            RuleFor(x => x.Description).MaximumLength(100).WithMessage("Proje acıklaması maksimum 500 karakter olabilir");

        }

    }
}
