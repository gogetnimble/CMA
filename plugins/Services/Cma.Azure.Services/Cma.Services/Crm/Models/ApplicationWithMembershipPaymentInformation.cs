namespace Cma.Services.Crm.Models;

public record ApplicationWithMembershipPaymentInformation(
    Guid Id,
    string ApplicationNumber,
    decimal GrandTotal,
    DateTime MembershipExpiryDate,
    string MembershipItemDescription,
    int TaxJurisdiction,
    decimal BaseMembershipCost,
    decimal Subtotal,
    decimal Discount,
    decimal Gst,
    decimal GstPercentage,
    decimal Hst,
    decimal HstPercentage,
    decimal Qst,
    decimal QstPercentage
)
    : Application(Id, ApplicationNumber);