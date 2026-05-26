using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class FindContactByTrackingContextIdRequestValidator : AbstractValidator<FindContactByTrackingContextIdRequest>
{
    public FindContactByTrackingContextIdRequestValidator()
    {
        RuleFor(x => x.TrackingContextId)
            .NotNull()
            .NotEmpty();
    }
}