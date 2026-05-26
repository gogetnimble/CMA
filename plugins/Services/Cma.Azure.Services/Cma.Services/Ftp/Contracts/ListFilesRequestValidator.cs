using FluentValidation;

namespace Cma.Services.Ftp.Contracts;

public class ListFilesRequestValidator : AbstractValidator<ListFilesRequest>
{
    public ListFilesRequestValidator()
    {
        RuleFor(x => x.Path)
            .NotNull()
            .NotEmpty();
    }
}