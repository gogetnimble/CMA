using Cma.Services.Crm.Attributes;

namespace Cma.Services.Crm.Contracts;


public record GetCmaMembershipDetailResponse(List<CmaMembershipDetail>? CmaMembershipDetails);
public record CmaMembershipDetail
{
    [CrmProperty(CrmConstants.CmaMembershipDetail.MembershipStatus)]
    public int? MembershipStatus { get; set; }

    [CrmProperty(CrmConstants.CmaMembershipDetail.ExpiryDate)]
    public DateTime? ExpiryDate { get; set; }

    [CrmProperty(CrmConstants.CmaMembershipDetail.MembershipYear)]
    public string? MembershipYear { get; set; }

    [CrmProperty(CrmConstants.CmaMembershipDetail.Category)]
    public Guid? Category { get; set; }
    
    [CrmLinkedEntity(CrmConstants.Order.EntityName, CrmConstants.CmaMembershipDetail.OrderId, CrmConstants.Order.Id, nameof(Order))]
    public Order? Order { get; set; }
    
    [CrmLinkedEntity(CrmConstants.Account.EntityName, CrmConstants.CmaMembershipDetail.Ptma, CrmConstants.Account.Id, nameof(Account))]
    public Account? Account { get; set; }
}

public record Order
{
    [CrmProperty(CrmConstants.Order.AmountOutstanding)]
    public string? AmountOutstanding { get; set; }

    [CrmProperty(CrmConstants.Order.LineItemDiscountAmount)]
    public string? LineItemDiscountAmount { get; set; }

    [CrmProperty(CrmConstants.Order.AmountPaid)]
    public string? AmountPaid { get; set; }

    [CrmProperty(CrmConstants.Order.TotalAmount)]
    public string? TotalAmount { get; set; }

    [CrmProperty(CrmConstants.Order.CreatedOn)]
    public DateTime? CreatedOn { get; set; }

    [CrmProperty(CrmConstants.Order.SubTotal)]
    public string? SubTotal { get; set; }

    [CrmProperty(CrmConstants.Order.Gst)]
    public string? Gst { get; set; }

    [CrmProperty(CrmConstants.Order.Hst)]
    public string? Hst { get; set; }

    [CrmProperty(CrmConstants.Order.Qst)]
    public string? Qst { get; set; }

    [CrmProperty(CrmConstants.Order.GstPercentage)]
    public string? GstPercentage { get; set; }

    [CrmProperty(CrmConstants.Order.HstPercentage)]
    public string? HstPercentage { get; set; }

    [CrmProperty(CrmConstants.Order.QstPercentage)]
    public string? QstPercentage { get; set; }

    [CrmProperty(CrmConstants.Order.TaxJurisdiction)]
    public int? TaxJurisdiction { get; set; }
}

public record Account
{
    [CrmProperty(CrmConstants.Account.Id)]
    public Guid? AccountId { get; set; }

    [CrmProperty(CrmConstants.Account.EnglishName)]
    public string? EnglishName { get; set; }

    [CrmProperty(CrmConstants.Account.FrenchName)]
    public string? FrenchName { get; set; }
}