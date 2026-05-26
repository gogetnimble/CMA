namespace Cma.Services.Sharepoint;

public class SharepointServiceConfiguration
{
    public SharepointServiceConfiguration(string tenantId, string clientId, string clientSecret)
    {
        TenantId = tenantId;
        ClientId = clientId;
        ClientSecret = clientSecret;
    }

    public string TenantId { get; }
    public string ClientId { get; }
    public string ClientSecret { get; }
}