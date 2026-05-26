using FluentValidation;

namespace Cma.Services.Ftp.Contracts;

public class GetFileRequestValidator : AbstractValidator<GetFileRequest>
{
    public GetFileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotNull()
            .NotEmpty();
    }
}