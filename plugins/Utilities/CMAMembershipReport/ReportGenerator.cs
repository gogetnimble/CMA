using System.Text;
using Newtonsoft.Json;

namespace CMA.Utilities.MembershipReport;

/// <summary>
/// Reads the HTML template shipped beside the executable, injects the dataset in
/// place of the <c>CMA_DATA_START … CMA_DATA_END</c> marker block, and writes the
/// finished, self-contained report.
/// </summary>
public sealed class ReportGenerator
{
    private const string StartMarker = "/* CMA_DATA_START */";
    private const string EndMarker   = "/* CMA_DATA_END */";
    private const string SchemaStart = "/* CMA_SCHEMA_START */";
    private const string SchemaEnd   = "/* CMA_SCHEMA_END */";

    private static readonly string TemplatePath =
        Path.Combine(AppContext.BaseDirectory, "Templates", "report-template.html");

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.None
    };

    /// <summary>Writes the report to <paramref name="outputPath"/> and returns its full path.</summary>
    public static string Write(
        IReadOnlyList<MembershipRecord> records,
        string outputPath,
        string environment,
        string? defaultYear,
        string statusFilter)
    {
        if (!File.Exists(TemplatePath))
            throw new FileNotFoundException(
                $"Report template not found at {TemplatePath}. " +
                "Ensure Templates/report-template.html is copied to the output directory.");

        var template = File.ReadAllText(TemplatePath);
        var generatedOn = DateTime.Now.ToString("o");

        var payload = new
        {
            meta = new
            {
                generatedOn,
                environment,
                defaultYear,
                statusFilter,
                source = "Dataverse (client-secret)"
            },
            records
        };

        var schema = ReportSchema.Build(environment, generatedOn);

        // JSON is emitted into <script> elements; the only break-out sequence is "</script",
        // so neutralise "</" defensively.
        var dataJson = JsonConvert.SerializeObject(payload, JsonSettings).Replace("</", "<\\/");
        var schemaJson = JsonConvert.SerializeObject(schema, JsonSettings).Replace("</", "<\\/");

        var html = Replace(template, StartMarker, EndMarker,
            $"{StartMarker}\nwindow.__CMA_DATA__ = {dataJson};\n{EndMarker}", "CMA_DATA");
        html = Replace(html, SchemaStart, SchemaEnd,
            $"{SchemaStart}\nwindow.__CMA_SCHEMA__ = {schemaJson};\n{SchemaEnd}", "CMA_SCHEMA");

        var full = Path.GetFullPath(outputPath);
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(full, html, new UTF8Encoding(false));

        // Sibling schema file: <report>.schema.json — the query, filters and fields for every report.
        var schemaPath = Path.ChangeExtension(full, null) + ".schema.json";
        File.WriteAllText(schemaPath,
            JsonConvert.SerializeObject(schema, Formatting.Indented), new UTF8Encoding(false));

        return full;
    }

    private static string Replace(string template, string startMarker, string endMarker, string injected, string name)
    {
        var start = template.IndexOf(startMarker, StringComparison.Ordinal);
        var end = template.IndexOf(endMarker, StringComparison.Ordinal);
        if (start < 0 || end < 0 || end < start)
            throw new InvalidOperationException($"Template is missing the {name}_START / {name}_END markers.");

        return new StringBuilder()
            .Append(template, 0, start)
            .Append(injected)
            .Append(template, end + endMarker.Length, template.Length - (end + endMarker.Length))
            .ToString();
    }
}
