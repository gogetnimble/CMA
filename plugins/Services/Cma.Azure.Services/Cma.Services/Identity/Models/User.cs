namespace Cma.Services.Identity.Models;

public record User(string Id, string CmahId, bool AccountLocked, string Username,bool IsCmaMember=false, List<string>? Emails=default);