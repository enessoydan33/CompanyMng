using CompanyMng.Dto.TaskDto;
using FluentValidation;

namespace CompanyMng.Validators.TaskDtaoValidators
{
    public class UpdateTaskStatusDtoValidator:AbstractValidator<UpdateTaskStatusDto>
    {
        public UpdateTaskStatusDtoValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Geçersiz task status değeri.");
        }
    }
}
