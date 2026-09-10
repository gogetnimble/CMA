using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMA.Utilities.MembershipReport;

/// <summary>
/// Runtime configuration for the membership report.
///
/// Resolution order (later wins):
///   1. appsettings.json next to the executable
///   2. Environment variables (CMA_DATAVERSE_URL, CMA_CLIENT_ID, CMA_CLIENT_SECRET,
///      CMA_TENANT_ID, CMA_MEMBERSHIP_YEAR, CMA_OUTPUT_PATH)
///   3. Command-line prompts (for anything still missing / non-secret)
///
/// The client secret is never persisted by this tool. Provide it through an
/// environment variable or appsettings.json that lives outside source control.
/// </summary>
public sealed class AppConfig
{
    /// <summary>Organization URL, e.g. https://yourorg.crm3.dynamics.com</summary>
    public string DataverseUrl { get; set; } = string.Empty;

    /// <summary>Entra (Azure AD) application (client) id of the app registration.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret for the app registration. Kept in memory only.</summary>
    [JsonIgnore]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Optional tenant id — lets MSAL skip a discovery round-trip.</summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Membership year to pre-select in the report. Null/empty means "let the
    /// report default to the most recent year found in the data".
    /// </summary>
    public string? MembershipYear { get; set; }

    /// <summary>Where the generated HTML report is written.</summary>
    public string OutputPath { get; set; } =
        Path.Combine(Directory.GetCurrentDirectory(), "cma-membership-report.html");

    // ── Loading ────────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static AppConfig Load()
    {
        var cfg = new AppConfig();

        // 1. appsettings.json (non-secret values; secret allowed but discouraged)
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(settingsPath))
        {
            try
            {
                var fromFile = JsonSerializer.Deserialize<AppConfig>(
                    File.ReadAllText(settingsPath), JsonOpts);
                if (fromFile is not null)
                {
                    cfg.DataverseUrl = fromFile.DataverseUrl;
                    cfg.ClientId = fromFile.ClientId;
                    cfg.TenantId = fromFile.TenantId;
                    cfg.MembershipYear = fromFile.MembershipYear;
                    if (!string.IsNullOrWhiteSpace(fromFile.OutputPath))
                        cfg.OutputPath = fromFile.OutputPath;
                    // Secret may be supplied here for unattended runs.
                    var secretFromFile = ReadRawSecret(settingsPath);
                    if (!string.IsNullOrWhiteSpace(secretFromFile))
                        cfg.ClientSecret = secretFromFile;
                }
            }
            catch { /* malformed file — fall through to env / prompts */ }
        }

        // 2. Environment variables override the file
        cfg.DataverseUrl = Env("CMA_DATAVERSE_URL", cfg.DataverseUrl);
        cfg.ClientId = Env("CMA_CLIENT_ID", cfg.ClientId);
        cfg.ClientSecret = Env("CMA_CLIENT_SECRET", cfg.ClientSecret);
        cfg.TenantId = Env("CMA_TENANT_ID", cfg.TenantId ?? string.Empty);
        cfg.MembershipYear = Env("CMA_MEMBERSHIP_YEAR", cfg.MembershipYear ?? string.Empty);
        cfg.OutputPath = Env("CMA_OUTPUT_PATH", cfg.OutputPath);

        cfg.DataverseUrl = cfg.DataverseUrl.TrimEnd('/');
        return cfg;
    }

    private static string Env(string key, string fallback)
    {
        var v = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
    }

    // Pull ClientSecret from the raw JSON even though the property is [JsonIgnore]
    // on read, so an appsettings.json used for unattended runs still works.
    private static string ReadRawSecret(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            if (doc.RootElement.TryGetProperty("ClientSecret", out var s) &&
                s.ValueKind == JsonValueKind.String)
                return s.GetString() ?? string.Empty;
        }
        catch { /* ignore */ }
        return string.Empty;
    }

    /// <summary>True when everything required to connect is present.</summary>
    public bool HasConnectionInfo =>
        !string.IsNullOrWhiteSpace(DataverseUrl) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
