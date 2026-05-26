using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class FindContactByCustomerInsightsTrackingIdRequestValidator: AbstractValidator<FindContactByCustomerInsightsTrackingIdRequest>
{
    public FindContactByCustomerInsightsTrackingIdRequestValidator()
    {
        RuleFor(x => x.TrackingId)
            .NotNull()
            .NotEmpty();
    }
    
}