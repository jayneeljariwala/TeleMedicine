namespace NHS111.Application.Validators;

using FluentValidation;
using NHS111.Application.DTOs.Requests;

public class CreateDispositionRequestValidator : AbstractValidator<CreateDispositionRequest>
{
    private static readonly string[] ValidUrgencies = ["Immediate", "UrgentSameDay", "Routine"];

    public CreateDispositionRequestValidator()
    {
        RuleFor(x => x.NhsNumber)
            .NotEmpty().WithMessage("NhsNumber is required.")
            .Length(10, 20).WithMessage("NhsNumber must be between 10 and 20 characters.");

        RuleFor(x => x.PatientName)
            .NotEmpty().WithMessage("PatientName is required.")
            .MaximumLength(200).WithMessage("PatientName cannot exceed 200 characters.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("DateOfBirth is required.")
            .LessThan(DateTime.UtcNow).WithMessage("DateOfBirth must be in the past.");

        RuleFor(x => x.DxCode)
            .NotEmpty().WithMessage("DxCode is required.")
            .Matches(@"^Dx\d{3}$").WithMessage("DxCode must match regex '^Dx\\d{3}$'.");

        RuleFor(x => x.DosCode)
            .NotEmpty().WithMessage("DosCode is required.")
            .MaximumLength(10).WithMessage("DosCode cannot exceed 10 characters.");

        RuleFor(x => x.Urgency)
            .NotEmpty().WithMessage("Urgency is required.")
            .Must(u => ValidUrgencies.Contains(u))
            .WithMessage("Urgency must be one of: Immediate, UrgentSameDay, Routine.");
    }
}
