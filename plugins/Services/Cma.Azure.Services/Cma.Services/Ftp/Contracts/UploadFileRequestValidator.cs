using FluentValidation;

namespace Cma.Services.Ftp.Contracts;

public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    public UploadFileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.FileBytes)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(x => x.Length > 0)
            .WithMessage("FileBytes must not be empty");
        
    }
}