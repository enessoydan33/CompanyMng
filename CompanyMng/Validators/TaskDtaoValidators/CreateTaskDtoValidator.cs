using FluentValidation;
using CompanyMng.Dto.TaskDto;
namespace CompanyMng.Validators.TaskDtaoValidators;

public class CreateTaskDtoValidator:AbstractValidator<CreateTaskDto>
{
    public CreateTaskDtoValidator()
    {
        RuleFor(x=> x.Title)
           .NotEmpty()
           .WithMessage("Task başlığı boş olamaz.")
           .MaximumLength(150)
           .WithMessage("Task başlığı en fazla 150 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Açıklama en fazla 1000 karakter olabilir.");
        RuleFor(x => x.Priority)
    .IsInEnum()
    .WithMessage("Geçersiz priority değeri.");
    }
}


