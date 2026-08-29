using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Spectre.Console;

namespace CMA.Utilities.MembershipReport;

/// <summary>
/// Reads <c>new_cmamembershipdetail</c> and resolves the three lookups the report
/// needs — Contact (fullname), Category (product name) and PTMA (account name) —
/// in a single paged query via outer joins.
/// </summary>
public sealed class MembershipRepository
{
    private const string Entity = "new_cmamembershipdetail";

    // Field logical names on the membership-detail table.
    private const string FldId       = "new_cmamembershipdetailid";
    private const string FldExpiry   = "new_expirydate";
    private const string FldYear     = "new_membershipyear";
    private const string FldStatus   = "new_status";
    private const string FldContact  = "new_contact";            // → contact
    private const string FldCategory = "new_categoryproductid";  // → product
    private const string FldPtma     = "new_divassocaccountid";  // → account
    private const string FldCreated  = "createdon";

    private readonly DataverseConnection _conn;

    public MembershipRepository(DataverseConnection conn) => _conn = conn;

    public async Task<List<MembershipRecord>> FetchAllAsync()
    {
        var query = new QueryExpression(Entity)
        {
            ColumnSet = new ColumnSet(
                FldId, FldExpiry, FldYear, FldStatus,
                FldContact, FldCategory, FldPtma, FldCreated),
            PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 },
            NoLock = true,
            Orders = { new OrderExpression(FldCreated, OrderType.Descending) }
        };

        // Outer joins so a record with a missing lookup still comes back.
        query.LinkEntities.Add(new LinkEntity(
            Entity, "contact", FldContact, "contactid", JoinOperator.LeftOuter)
        { Columns = new ColumnSet("fullname"), EntityAlias = "con" });

        query.LinkEntities.Add(new LinkEntity(
            Entity, "product", FldCategory, "productid", JoinOperator.LeftOuter)
        { Columns = new ColumnSet("name"), EntityAlias = "prod" });

        query.LinkEntities.Add(new LinkEntity(
            Entity, "account", FldPtma, "accountid", JoinOperator.LeftOuter)
        { Columns = new ColumnSet("name"), EntityAlias = "acct" });

        var records = new List<MembershipRecord>();

        while (true)
        {
            var page = query.PageInfo.PageNumber;
            var result = await _conn.ExecuteAsync(
                svc => svc.RetrieveMultiple(query),
                $"RetrieveMultiple(page {page})");

            foreach (var e in result.Entities)
                records.Add(Map(e));

            AnsiConsole.MarkupLine(
                $"  [grey]▸ page {page,-3}[/] [dodgerblue2]{result.Entities.Count,5:N0}[/] [grey]rows[/]");

            if (!result.MoreRecords) break;

            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = result.PagingCookie;
        }

        return records;
    }

    private static MembershipRecord Map(Entity e)
    {
        var created = e.GetAttributeValue<DateTime?>(FldCreated);
        var expiry = e.GetAttributeValue<DateTime?>(FldExpiry);

        return new MembershipRecord
        {
            Id       = e.Id.ToString(),
            Contact  = Aliased(e, "con.fullname") ?? "(no contact)",
            Category = Aliased(e, "prod.name")    ?? "(no category)",
            Ptma     = Aliased(e, "acct.name")    ?? "(no PTMA)",
            Expiry   = expiry?.ToString("yyyy-MM-dd"),
            Year     = ReadYear(e),
            Status   = ReadStatusLabel(e),
            Created  = (created ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
            CreatedMonth = (created?.Month ?? 1) - 1
        };
    }

    // Aliased join columns arrive wrapped in AliasedValue.
    private static string? Aliased(Entity e, string key) =>
        e.Contains(key) && e[key] is AliasedValue av
            ? av.Value?.ToString()
            : null;

    // new_membershipyear may be text, a whole number, an option set, or a lookup.
    // FormattedValues covers option-set/lookup/number display; fall back to raw.
    private static string ReadYear(Entity e)
    {
        if (e.FormattedValues.Contains(FldYear) &&
            !string.IsNullOrWhiteSpace(e.FormattedValues[FldYear]))
            return e.FormattedValues[FldYear];

        if (!e.Contains(FldYear)) return string.Empty;

        return e[FldYear] switch
        {
            EntityReference r => r.Name ?? string.Empty,
            OptionSetValue o  => o.Value.ToString(),
            DateTime d        => d.Year.ToString(),
            _                 => e[FldYear]?.ToString() ?? string.Empty
        };
    }

    // Option-set label ("Active", "Lapsed", …) via FormattedValues.
    private static string ReadStatusLabel(Entity e)
    {
        if (e.FormattedValues.Contains(FldStatus) &&
            !string.IsNullOrWhiteSpace(e.FormattedValues[FldStatus]))
            return e.FormattedValues[FldStatus];

        return e.GetAttributeValue<OptionSetValue>(FldStatus)?.Value.ToString()
               ?? "(no status)";
    }
}
