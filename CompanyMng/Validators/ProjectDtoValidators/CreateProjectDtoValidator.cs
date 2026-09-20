using CompanyMng.Dto.ProjectDto;
using FluentValidation;
using System.Data;

namespace CompanyMng.Validators.ProjectDtoValidators
{
    public class CreateProjectDtoValidator:AbstractValidator<CreateProjectDto>
    {

        public CreateProjectDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Proje adı boş Olamaz")
                .MaximumLength(100).WithMessage("Proje adı maksimum 100 karakter olabilir");
            RuleFor(x => x.Description).MaximumLength(100).WithMessage("Proje acıklaması maksimum 500 karakter olabilir");


        }
        
    }
}
