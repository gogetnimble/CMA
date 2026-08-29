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

        var payload = new
        {
            meta = new
            {
                generatedOn = DateTime.Now.ToString("o"),
                environment,
                defaultYear,
                statusFilter,
                source = "Dataverse (client-secret)"
            },
            records
        };

        // The JSON is emitted into a <script> element. The only sequence that can
        // break out of it is "</script"; neutralise it defensively.
        var json = JsonConvert.SerializeObject(payload, JsonSettings)
            .Replace("</", "<\\/");

        var injected = $"{StartMarker}\nwindow.__CMA_DATA__ = {json};\n{EndMarker}";

        var start = template.IndexOf(StartMarker, StringComparison.Ordinal);
        var end = template.IndexOf(EndMarker, StringComparison.Ordinal);
        if (start < 0 || end < 0 || end < start)
            throw new InvalidOperationException(
                "Template is missing the CMA_DATA_START / CMA_DATA_END markers.");

        var html = new StringBuilder()
            .Append(template, 0, start)
            .Append(injected)
            .Append(template, end + EndMarker.Length, template.Length - (end + EndMarker.Length))
            .ToString();

        var full = Path.GetFullPath(outputPath);
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(full, html, new UTF8Encoding(false));
        return full;
    }
}
