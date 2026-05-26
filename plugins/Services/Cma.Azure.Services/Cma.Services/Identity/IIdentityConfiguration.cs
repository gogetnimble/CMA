namespace Cma.Services.Identity;

public interface IIdentityConfiguration
{
    public string Wso2BaseAddress { get; }
    public string Wso2AdminUser { get; }
    public string Wso2AdminPassword { get; }
}