namespace Cma.Services.Identity;

public record IdentityConfiguration(string Wso2BaseAddress,string Wso2AdminUser,string Wso2AdminPassword):IIdentityConfiguration;