namespace NHS111.Application.Validators;

using FluentValidation;
using NHS111.Application.DTOs.Requests;

public class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.NhsNumber)
            .NotEmpty().WithMessage("NhsNumber is required.")
            .Length(10, 20).WithMessage("NhsNumber must be between 10 and 20 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("DateOfBirth is required.")
            .LessThan(DateTime.UtcNow).WithMessage("DateOfBirth must be in the past.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
