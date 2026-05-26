namespace Cma.Services.Crm;

public record CrmServiceConfiguration(
    Guid EmailQueueIdEnglish,
    Guid EmailQueueIdFrench,
    Guid RealTimeMarketingPurposeId);