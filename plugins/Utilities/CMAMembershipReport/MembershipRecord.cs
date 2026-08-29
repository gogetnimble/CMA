using Newtonsoft.Json;

namespace CMA.Utilities.MembershipReport;

/// <summary>
/// One flattened membership-detail row, shaped for the report's client-side JS.
/// Property names are the exact keys the HTML template reads, so this serializes
/// straight into <c>window.__CMA_DATA__.records</c>.
/// </summary>
public sealed class MembershipRecord
{
    [JsonProperty("id")]        public string Id { get; set; } = string.Empty;

    /// <summary>new_Contact → contact.fullname</summary>
    [JsonProperty("contact")]   public string Contact { get; set; } = "(no contact)";

    /// <summary>new_Categoryproductid → product.name</summary>
    [JsonProperty("category")]  public string Category { get; set; } = "(no category)";

    /// <summary>new_divassocaccountid → account.name</summary>
    [JsonProperty("ptma")]      public string Ptma { get; set; } = "(no PTMA)";

    /// <summary>new_ExpiryDate as yyyy-MM-dd (null when unset).</summary>
    [JsonProperty("expiry")]    public string? Expiry { get; set; }

    /// <summary>new_MembershipYear as a display string.</summary>
    [JsonProperty("year")]      public string Year { get; set; } = string.Empty;

    /// <summary>new_Status option-set label (e.g. "Active").</summary>
    [JsonProperty("status")]    public string Status { get; set; } = string.Empty;

    /// <summary>createdon as yyyy-MM-dd.</summary>
    [JsonProperty("created")]   public string Created { get; set; } = string.Empty;

    /// <summary>createdon month index, 0 = Jan … 11 = Dec (drives the by-month chart).</summary>
    [JsonProperty("createdMonth")] public int CreatedMonth { get; set; }
}
