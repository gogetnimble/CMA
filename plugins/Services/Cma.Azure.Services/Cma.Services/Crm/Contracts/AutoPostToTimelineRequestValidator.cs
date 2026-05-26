using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class AutoPostToTimelineRequestValidator : AbstractValidator<AutoPostToTimelineRequest>
{
    public AutoPostToTimelineRequestValidator()
    {
        RuleFor(request => request.ContactId).NotEmpty();
        RuleFor(request => request.Message).NotEmpty().MaximumLength(2000);
    }
}