using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace CMA.Utilities.DataverseExporter;

public enum ExportMode { Verbose, Simple }

public class Exporter
{
    // ── Tables ────────────────────────────────────────────────────────────────
    private static readonly string[] Tables =
    [
        "new_accounttype",
        "new_attendeetype",
        "new_cmatag",
        "new_codelookup",
        "new_contacttype",
        "new_discontinuedreason",
        "new_eventtype",
        "new_opportunitytype",
        "new_specialty",
        "new_cmaflowconfiguration",
        "new_customconfiguration",
        "new_taxrate",
        "product",
        "productassociation",
        "productsubstitute",
        "pricelevel",
        "productpricelevel",
        "new_optmanagementscreen",
        "new_membershippricingrule"
    ];

    private readonly string _dataverseUrl;
    private readonly string _outputDir;

    public Exporter(string dataverseUrl, string outputDir)
    {
        _dataverseUrl = dataverseUrl;
        _outputDir = outputDir;
    }

    // ── Entry point ───────────────────────────────────────────────────────────

    public async Task RunAsync(ExportMode mode)
    {
        AnsiConsole.Markup("[grey]Connecting...[/] ");

        using var client = BuildClient();

        if (!client.IsReady)
            throw new InvalidOperationException(
                $"Could not connect to Dataverse. Last error: {client.LastError}");

        AnsiConsole.MarkupLine("[green]Connected ✓[/]");
        AnsiConsole.WriteLine();

        int totalRecords = 0;
        int succeeded = 0;
        int failed = 0;

        foreach (var table in Tables)
        {
            AnsiConsole.Markup($"  [grey]▸[/] [dodgerblue2]{table,-45}[/]");

            try
            {
                var (count, filePath, _) = await ExportTableAsync(client, table, mode);
                AnsiConsole.MarkupLine(
                    $"[green]{count,6:N0}[/] [grey]rows  →  {Path.GetFileName(filePath)}[/]");
                totalRecords += count;
                succeeded++;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]FAILED[/]  [dim]{Markup.Escape(ex.Message)}[/]");
                failed++;
            }
        }

        // ── Summary ───────────────────────────────────────────────────────────
        AnsiConsole.WriteLine();

        var t = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse("grey"))
            .AddColumn(new TableColumn("[grey]Metric[/]"))
            .AddColumn(new TableColumn("[grey]Value[/]").RightAligned());

        t.AddRow("[white]Tables succeeded[/]", $"[green]{succeeded}[/]");

        if (failed > 0)
            t.AddRow("[white]Tables failed[/]", $"[red]{failed}[/]");

        t.AddRow("[white]Total records[/]", $"[white]{totalRecords:N0}[/]");
        t.AddRow("[white]Output folder[/]", $"[grey]{Markup.Escape(_outputDir)}[/]");

        AnsiConsole.Write(t);
    }

    // ── Per-table export ──────────────────────────────────────────────────────

    /// <summary>
    /// Exports a single table and returns (recordCount, filePath, entitySetName).
    /// Internal visibility so PatchGenerator can call it standalone if needed.
    /// </summary>
    internal async Task<(int count, string filePath, string entitySetName)> ExportTableAsync(
        ServiceClient client,
        string logicalName,
        ExportMode mode)
    {
        // 1. Metadata
        var metaResp = (RetrieveEntityResponse)await Task.Run(() =>
            client.Execute(new RetrieveEntityRequest
            {
                LogicalName = logicalName,
                EntityFilters = EntityFilters.Attributes,
                RetrieveAsIfPublished = true
            }));

        // EntitySetName is the OData collection name (e.g. "new_accounttypes")
        var entitySetName = metaResp.EntityMetadata.EntitySetName ?? logicalName + "s";

        // 2. Build column set — exclude virtual/child attributes
        var allAttributes = metaResp.EntityMetadata.Attributes
            .Where(a => string.IsNullOrEmpty(a.AttributeOf))
            .Where(a => a.IsValidForRead == true)
            .Select(a => a.LogicalName)
            .ToArray();

        // 3. Page through all records
        var records = new List<JObject>();
        var query = new QueryExpression(logicalName)
        {
            ColumnSet = new ColumnSet(allAttributes),
            PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 }
        };

        // For productpricelevel, only export records linked to active products.
        // Retired products cannot be added to a price list in the target environment.
        if (logicalName.Equals("productpricelevel", StringComparison.OrdinalIgnoreCase))
        {
            query.LinkEntities.Add(new LinkEntity
            {
                LinkFromEntityName = "productpricelevel",
                LinkFromAttributeName = "productid",
                LinkToEntityName = "product",
                LinkToAttributeName = "productid",
                JoinOperator = JoinOperator.Inner,
                LinkCriteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                    }
                }
            });
        }

        while (true)
        {
            var result = await Task.Run(() => client.RetrieveMultiple(query));

            foreach (var entity in result.Entities)
                records.Add(mode == ExportMode.Simple
                    ? EntityToJObjectSimple(entity)
                    : EntityToJObjectVerbose(entity));

            if (!result.MoreRecords) break;

            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = result.PagingCookie;
        }

        // 4. Detect self-referential lookup fields and topological sort
        var selfRefFields = metaResp.EntityMetadata.Attributes
            .OfType<LookupAttributeMetadata>()
            .Where(a => string.IsNullOrEmpty(a.AttributeOf))
            .Where(a => a.IsValidForRead == true)
            .Where(a => a.Targets != null && a.Targets.Contains(logicalName))
            .Select(a => a.LogicalName)
            .ToArray();

        if (selfRefFields.Length > 0)
            records = TopologicalSort(records, selfRefFields);

        // 5. Write JSON envelope
        var filePath = Path.Combine(_outputDir, $"{logicalName}.json");
        var json = JsonConvert.SerializeObject(
            new
            {
                table = logicalName,
                entitySetName,                              // <-- stored for PatchGenerator
                exportedUtc = DateTime.UtcNow.ToString("o"),
                exportMode = mode.ToString().ToLower(),
                selfRefFields,
                totalRecords = records.Count,
                records
            },
            Formatting.Indented);

        await File.WriteAllTextAsync(filePath, json);
        return (records.Count, filePath, entitySetName);
    }

    // ── Simple serializer ─────────────────────────────────────────────────────
    // Lookups → id + _name/_logicalname siblings, OptionSets → raw int + _label sibling

    private static JObject EntityToJObjectSimple(Entity entity)
    {
        var obj = new JObject { ["id"] = entity.Id.ToString() };

        foreach (var attr in entity.Attributes)
        {
            switch (attr.Value)
            {
                case null:
                    obj[attr.Key] = JValue.CreateNull();
                    break;

                case EntityReference er:
                    obj[attr.Key] = er.Id.ToString();
                    obj[attr.Key + "_name"] = er.Name;
                    obj[attr.Key + "_logicalname"] = er.LogicalName;
                    break;

                case OptionSetValue osv:
                    obj[attr.Key] = osv.Value;
                    if (!IsStateField(attr.Key) &&
                        entity.FormattedValues.TryGetValue(attr.Key, out var lbl))
                        obj[attr.Key + "_label"] = lbl;
                    break;

                case OptionSetValueCollection osvc:
                    obj[attr.Key] = new JArray(osvc.Select(v => v.Value));
                    break;

                case Money m:
                    obj[attr.Key] = m.Value;
                    break;

                case AliasedValue av:
                    obj[attr.Key] = av.Value is null
                        ? JValue.CreateNull()
                        : JToken.FromObject(av.Value);
                    break;

                case string s: obj[attr.Key] = s; break;
                case bool b: obj[attr.Key] = b; break;
                case int i: obj[attr.Key] = i; break;
                case long l: obj[attr.Key] = l; break;
                case decimal d: obj[attr.Key] = d; break;
                case double dbl: obj[attr.Key] = dbl; break;
                case DateTime dt: obj[attr.Key] = dt.ToString("o"); break;
                case Guid g: obj[attr.Key] = g.ToString(); break;

                default:
                    obj[attr.Key] = JToken.FromObject(attr.Value);
                    break;
            }
        }

        return obj;
    }

    private static bool IsStateField(string fieldName) =>
        fieldName.Equals("statecode", StringComparison.OrdinalIgnoreCase) ||
        fieldName.Equals("statuscode", StringComparison.OrdinalIgnoreCase);

    // ── Verbose serializer ────────────────────────────────────────────────────
    // Lookups → { id, logicalName, name }, OptionSets → { value, label }

    private static JObject EntityToJObjectVerbose(Entity entity)
    {
        var obj = new JObject { ["id"] = entity.Id.ToString() };

        foreach (var attr in entity.Attributes)
        {
            switch (attr.Value)
            {
                case null:
                    obj[attr.Key] = JValue.CreateNull();
                    break;

                case EntityReference er:
                    obj[attr.Key] = new JObject
                    {
                        ["id"] = er.Id.ToString(),
                        ["logicalName"] = er.LogicalName,
                        ["name"] = er.Name
                    };
                    break;

                case OptionSetValue osv:
                    entity.FormattedValues.TryGetValue(attr.Key, out var fv);
                    obj[attr.Key] = new JObject
                    {
                        ["value"] = osv.Value,
                        ["label"] = fv
                    };
                    break;

                case OptionSetValueCollection osvc:
                    obj[attr.Key] = new JArray(osvc.Select(v =>
                    {
                        entity.FormattedValues.TryGetValue(attr.Key, out var vLbl);
                        return new JObject { ["value"] = v.Value, ["label"] = vLbl };
                    }));
                    break;

                case Money m:
                    obj[attr.Key] = new JObject { ["value"] = m.Value };
                    break;

                case AliasedValue av:
                    obj[attr.Key] = av.Value is null
                        ? JValue.CreateNull()
                        : JToken.FromObject(av.Value);
                    break;

                case string s: obj[attr.Key] = s; break;
                case bool b: obj[attr.Key] = b; break;
                case int i: obj[attr.Key] = i; break;
                case long l: obj[attr.Key] = l; break;
                case decimal d: obj[attr.Key] = d; break;
                case double dbl: obj[attr.Key] = dbl; break;
                case DateTime dt: obj[attr.Key] = dt.ToString("o"); break;
                case Guid g: obj[attr.Key] = g.ToString(); break;

                default:
                    obj[attr.Key] = JToken.FromObject(attr.Value);
                    break;
            }
        }

        return obj;
    }

    // ── Topological sort (Kahn's algorithm) ───────────────────────────────────

    private static List<JObject> TopologicalSort(List<JObject> records, string[] selfRefFields)
    {
        var recordById = records
            .Where(r => r["id"] != null)
            .ToDictionary(r => r["id"]!.ToString(), r => r);

        var inDegree = recordById.Keys.ToDictionary(k => k, _ => 0);
        var dependents = recordById.Keys.ToDictionary(k => k, _ => new List<string>());

        foreach (var record in records)
        {
            var childId = record["id"]?.ToString();
            if (childId is null) continue;

            foreach (var field in selfRefFields)
            {
                var token = record[field];
                if (token is null || token.Type == JTokenType.Null) continue;

                // Simple mode stores a GUID string; Verbose stores { id: "..." }
                var parentId = token.Type == JTokenType.Object
                    ? token["id"]?.ToString()
                    : token.ToString();

                if (parentId is null || !recordById.ContainsKey(parentId)) continue;
                if (parentId == childId) continue;

                inDegree[childId]++;
                dependents[parentId].Add(childId);
            }
        }

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<JObject>(records.Count);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            sorted.Add(recordById[id]);

            foreach (var dep in dependents[id])
                if (--inDegree[dep] == 0)
                    queue.Enqueue(dep);
        }

        // Append records caught in a cycle rather than dropping them
        var cycled = records.Where(r => !sorted.Contains(r)).ToList();
        if (cycled.Count > 0)
        {
            AnsiConsole.MarkupLine(
                $"\n  [yellow]⚠[/]  [yellow]{cycled.Count} records in a circular reference — appended at end[/]");
            sorted.AddRange(cycled);
        }

        return sorted;
    }

    // ── Auth ──────────────────────────────────────────────────────────────────

    private ServiceClient BuildClient()
    {
        // Uses the well-known PowerShell public client — no app registration needed.
        const string clientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";
        const string redirectUri = "http://localhost";

        // Including TenantId lets MSAL find the cached token without an extra
        // discovery round-trip, so subsequent runs skip the browser login prompt.
        var connStr =
            $"AuthType=OAuth;" +
            $"Url={_dataverseUrl};" +
            $"AppId={clientId};" +
            $"RedirectUri={redirectUri};" +
            $"LoginPrompt=Auto;" +
            $"RequireNewInstance=False";

        return new ServiceClient(connStr);
    }
}