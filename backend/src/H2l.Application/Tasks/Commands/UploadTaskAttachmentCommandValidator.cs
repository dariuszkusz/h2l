using FluentValidation;

namespace H2l.Application.Tasks.Commands;

public sealed class UploadTaskAttachmentCommandValidator : AbstractValidator<UploadTaskAttachmentCommand>
{
    public UploadTaskAttachmentCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty();

        RuleFor(x => x.FieldVariableKey)
            .NotEmpty()
            .Matches("^[a-zA-Z][a-zA-Z0-9_]{0,63}$");

        RuleFor(x => x.OriginalFileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0);

        RuleFor(x => x.Content)
            .NotNull()
            .Must(c => c.Length > 0)
            .WithMessage("Uploaded file content must not be empty.");
    }
}
