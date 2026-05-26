using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace CMA.Utilities.DataverseExporter;

/// <summary>
/// Reads exported JSON files and produces PATCH-ready payload files.
/// Each output record contains a relative URL and a clean body with
/// all read-only / system fields stripped, and lookups formatted as
/// @odata.bind so the Dataverse Web API accepts them.
///
/// Output format per table  (e.g. new_accounttype_patch.json):
/// {
///   "table":         "new_accounttype",
///   "entitySetName": "new_accounttypes",
///   "generatedUtc":  "...",
///   "totalRecords":  12,
///   "records": [
///     {
///       "id":          "guid",
///       "relativeUrl": "/api/data/v9.2/new_accounttypes(guid)",
///       "body": {
///         "new_name": "...",
///         "new_parenttypeid@odata.bind": "/new_eventtypes(guid)"
///       }
///     }
///   ]
/// }
///
/// In Power Automate: load the patch file, iterate records,
/// then PATCH to  {{envUrl}}{{record.relativeUrl}}  with  {{record.body}}.
/// </summary>
public class PatchGenerator
{
    // ── Fields that are never writable via PATCH ──────────────────────────────
    private static readonly HashSet<string> SystemFields = new(StringComparer.OrdinalIgnoreCase)
    {
        // Currency — resolved dynamically in the flow against the target environment
        "transactioncurrencyid",

        // Unit of Measure — GUIDs differ per environment, appended dynamically in flow
        "uomid",
        "uomscheduleid",
        "defaultuomid",
        "defaultuomscheduleid",

        // Org / audit
        "organizationid",
        "createdby",
        "modifiedby",
        "createdon",
        "modifiedon",
        "overriddencreatedon",
        "versionnumber",

        // Ownership — default to service account in target env
        "ownerid",
        "owninguser",
        "owningteam",
        "owningbusinessunit",

        // System / solution
        "timezoneruleversionnumber",
        "utcconversiontimezonecode",
        "importsequencenumber",
        "solutionid",
        "supportingsolutionid",
        "componentstate",
        "overwritetime",
        "ismanaged",

        // BPF / process fields — zero GUIDs, not portable
        "processid",
        "stageid",
        "traversedpath",

        // Solution component metadata — read-only, not writable via PATCH
        "componentidunique",
        "iscustomizable",

        // CI-J / compliance lookups on msdynmkt_topic — env-specific, linked manually
        "msdynmkt_purposeid",
        "new_compliancecenter",
    };

    private readonly string _exportDir;
    private readonly string _patchDir;

    public PatchGenerator(string exportDir, string patchDir)
    {
        _exportDir = exportDir;
        _patchDir = patchDir;
    }

    // ── Entry point ───────────────────────────────────────────────────────────

    public async Task RunAsync()
    {
        var files = Directory.GetFiles(_exportDir, "*.json")
            .OrderBy(f => f)
            .ToArray();

        if (files.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠  No JSON export files found in:[/]");
            AnsiConsole.MarkupLine($"   [grey]{Markup.Escape(_exportDir)}[/]");
            return;
        }

        // Build logicalName → entitySetName map from ALL export files first.
        // Used to format lookup @odata.bind values with correct collection names.
        var entitySetMap = await BuildEntitySetMapAsync(files);

        int totalFiles = 0;
        int totalRecords = 0;

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            AnsiConsole.Markup($"  [grey]▸[/] [gold1]{Markup.Escape(fileName),-50}[/]");

            try
            {
                var (recordCount, outPath) = await ProcessFileAsync(file, entitySetMap);
                AnsiConsole.MarkupLine(
                    $"[green]{recordCount,6:N0}[/] [grey]records  →  {Path.GetFileName(outPath)}[/]");

                totalFiles++;
                totalRecords += recordCount;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]FAILED[/]  [dim]{Markup.Escape(ex.Message)}[/]");
            }
        }

        // ── Summary ───────────────────────────────────────────────────────────
        AnsiConsole.WriteLine();

        var t = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse("grey"))
            .AddColumn(new TableColumn("[grey]Metric[/]"))
            .AddColumn(new TableColumn("[grey]Value[/]").RightAligned());

        t.AddRow("[white]PATCH files generated[/]", $"[green]{totalFiles}[/]");
        t.AddRow("[white]Total records[/]", $"[white]{totalRecords:N0}[/]");
        t.AddRow("[white]Output folder[/]", $"[grey]{Markup.Escape(_patchDir)}[/]");

        AnsiConsole.Write(t);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Usage: in Power Automate, parse the patch file, iterate records,[/]");
        AnsiConsole.MarkupLine("[dim]then HTTP PATCH to[/] [white]{{envUrl}}{{record.relativeUrl}}[/] [dim]with body[/] [white]{{record.body}}[/]");
    }

    // ── Build entitySetName map from all export files ─────────────────────────
    //
    // Scans every *.json in the export folder and records the
    // logicalName → entitySetName stored in each file's envelope.
    // Falls back to logicalName + "s" for lookup targets not in the set.

    private static async Task<Dictionary<string, string>> BuildEntitySetMapAsync(string[] files)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            try
            {
                var root = JObject.Parse(await File.ReadAllTextAsync(file));
                var table = root["table"]?.Value<string>();
                var esn = root["entitySetName"]?.Value<string>();

                if (table != null && esn != null)
                    map[table] = esn;
            }
            catch { /* skip unparseable files */ }
        }

        return map;
    }

    // ── Process a single export file ──────────────────────────────────────────

    private async Task<(int recordCount, string outPath)> ProcessFileAsync(
        string file,
        Dictionary<string, string> entitySetMap)
    {
        var root = JObject.Parse(await File.ReadAllTextAsync(file));

        var tableName = root["table"]?.Value<string>()
                         ?? Path.GetFileNameWithoutExtension(file);
        var entitySetName = root["entitySetName"]?.Value<string>()
                         ?? tableName + "s";

        var sourceRecords = root["records"] as JArray ?? [];
        var primaryKey = tableName + "id";

        var patchRecords = new JArray();

        foreach (JObject record in sourceRecords.Cast<JObject>())
        {
            var id = record["id"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(id)) continue;

            var body = new JObject();

            foreach (var prop in record.Properties())
            {
                if (ShouldSkip(prop.Name, primaryKey)) continue;

                // Skip _name fields that are lookup display name siblings.
                // A _name field is a sibling if its base field (name minus "_name") exists in the record.
                // e.g. "owningbusinessunit_name" → base "owningbusinessunit" exists → skip.
                //      "new_name"               → base "new" does not exist       → keep.
                if (prop.Name.EndsWith("_name", StringComparison.OrdinalIgnoreCase))
                {
                    var baseName = prop.Name[..^"_name".Length];
                    if (record[baseName] != null) continue;
                }

                // Detect lookups by checking for a _logicalname sibling in the record.
                // Dataverse Web API requires lookups as @odata.bind, not raw GUIDs.
                var targetLogicalName = record[prop.Name + "_logicalname"]?.Value<string>();

                if (targetLogicalName != null)
                {
                    var guidValue = prop.Value.Type != JTokenType.Null
                        ? prop.Value.Value<string>()
                        : null;

                    if (string.IsNullOrWhiteSpace(guidValue))
                    {
                        // Null lookup — send null to clear the field
                        body[prop.Name] = JValue.CreateNull();
                    }
                    else
                    {
                        // Resolve entity set name: prefer export map, fall back to logicalName + "s"
                        var targetEntitySet = entitySetMap.TryGetValue(targetLogicalName, out var esn)
                            ? esn
                            : targetLogicalName + "s";

                        body[$"{prop.Name}@odata.bind"] = $"/{targetEntitySet}({guidValue})";
                    }
                }
                else
                {
                    // Trim datetime values to date-only for Edm.Date fields.
                    // Newtonsoft parses "2019-10-31T00:00:00" as JTokenType.Date automatically.
                    // Dataverse rejects datetime strings for date-only columns.
                    var val = prop.Value;
                    if (val.Type == JTokenType.Date)
                    {
                        var dt = val.Value<DateTime>();
                        if (dt.TimeOfDay == TimeSpan.Zero)
                            val = dt.ToString("yyyy-MM-dd");
                    }
                    body[prop.Name] = val;
                }
            }

            patchRecords.Add(new JObject
            {
                ["id"] = id,
                ["relativeUrl"] = $"/api/data/v9.2/{entitySetName}({id})",
                ["body"] = body
            });
        }

        var output = new JObject
        {
            ["table"] = tableName,
            ["entitySetName"] = entitySetName,
            ["generatedUtc"] = DateTime.UtcNow.ToString("o"),
            ["totalRecords"] = patchRecords.Count,
            ["records"] = patchRecords
        };

        var outPath = Path.Combine(_patchDir, $"{tableName}_patch.json");
        await File.WriteAllTextAsync(outPath, output.ToString(Formatting.Indented));

        return (patchRecords.Count, outPath);
    }

    // ── Field filter ─────────────────────────────────────────────────────────

    private static bool ShouldSkip(string fieldName, string primaryKeyField)
    {
        // Primary key goes in the URL, never in the body
        if (fieldName.Equals("id", StringComparison.OrdinalIgnoreCase)) return true;
        if (fieldName.Equals(primaryKeyField, StringComparison.OrdinalIgnoreCase)) return true;

        // Annotation siblings written by the exporter — not real Dataverse fields.
        // _logicalname and _label are always annotation suffixes — safe to strip unconditionally.
        // _name is handled in the caller loop (needs record context to distinguish sibling vs real field).
        if (fieldName.EndsWith("_logicalname", StringComparison.OrdinalIgnoreCase)) return true;
        if (fieldName.EndsWith("_label", StringComparison.OrdinalIgnoreCase)) return true;

        // Known system / read-only fields
        return SystemFields.Contains(fieldName);
    }
}