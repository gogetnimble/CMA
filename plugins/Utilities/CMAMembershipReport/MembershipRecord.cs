using Newtonsoft.Json;

namespace CMA.Utilities.MembershipReport;

/// <summary>
/// One flattened membership-detail row, shaped for the report's client-side JS.
/// Property names are the exact keys the HTML template reads, so this serializes
/// straight into <c>window.__CMA_DATA__.records</c>.
/// </summary>
public sealed class MembershipRecord
{
    /// <summary>
    /// Membership-detail id. Not serialized — no report uses it, so it's kept out of
    /// the embedded payload to save bytes. Re-add [JsonProperty] here if a future
    /// report needs to deep-link back to the record in CRM.
    /// </summary>
    [JsonIgnore]                public string Id { get; set; } = string.Empty;

    /// <summary>new_Contact lookup id — used to de-duplicate members in the demographics report.</summary>
    [JsonProperty("contactId")] public string? ContactId { get; set; }

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

    /// <summary>
    /// createdon month index, 0 = Jan … 11 = Dec. Not serialized — the report derives
    /// it from <see cref="Created"/>, so embedding it would just duplicate that byte-for-byte.
    /// </summary>
    [JsonIgnore]                public int CreatedMonth { get; set; }

    // ── Contact demographics (for the Demographics report) ──────────────────────

    /// <summary>contact.gendercode option-set label (e.g. "Male" / "Female"), null when unset.</summary>
    [JsonProperty("gender")]    public string? Gender { get; set; }

    /// <summary>contact.new_age (whole number), null when unset.</summary>
    [JsonProperty("age")]       public int? Age { get; set; }

    /// <summary>contact.new_language option-set label (e.g. "English" / "French"), null when unset.</summary>
    [JsonProperty("language")]  public string? Language { get; set; }
}
