using System.Diagnostics;
using CMA.Utilities.MembershipReport;
using Spectre.Console;

// ─── Banner ───────────────────────────────────────────────────────────────────
AnsiConsole.Write(
    new FigletText("CMA Report")
        .Centered()
        .Color(Color.SpringGreen3));
AnsiConsole.Write(
    new Rule("[grey]Membership report  ·  new_cmamembershipdetail  →  interactive HTML[/]")
        .RuleStyle(Style.Parse("grey dim"))
        .Centered());
AnsiConsole.WriteLine();

// ─── Config ───────────────────────────────────────────────────────────────────
var config = AppConfig.Load();

// Prompt only for what's missing and non-secret; keep unattended runs prompt-free.
if (string.IsNullOrWhiteSpace(config.DataverseUrl))
    config.DataverseUrl = AnsiConsole
        .Ask("[springgreen3]Dataverse URL[/]:", "https://yourorg.crm3.dynamics.com")
        .TrimEnd('/');

if (string.IsNullOrWhiteSpace(config.ClientId))
    config.ClientId = AnsiConsole.Ask<string>("[springgreen3]Client (application) id[/]:");

if (string.IsNullOrWhiteSpace(config.ClientSecret))
    config.ClientSecret = AnsiConsole.Prompt(
        new TextPrompt<string>("[springgreen3]Client secret[/]:").Secret());

if (!config.HasConnectionInfo)
{
    AnsiConsole.MarkupLine("[red]Missing connection details — need URL, client id and secret.[/]");
    return 1;
}

AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule($"[springgreen3]Connecting[/]  [white]{Markup.Escape(config.DataverseUrl)}[/]").RuleStyle("grey"));
AnsiConsole.WriteLine();

// ─── Connect + fetch + generate ────────────────────────────────────────────────
try
{
    using var conn = await DataverseConnection.ConnectAsync(config);
    AnsiConsole.MarkupLine($"  [green]Connected ✓[/]  [grey]{Markup.Escape(conn.ConnectedOrgUrl)}[/]");
    AnsiConsole.WriteLine();

    AnsiConsole.MarkupLine("[grey]Reading membership detail records…[/]");
    var repo = new MembershipRepository(conn);
    var records = await repo.FetchAllAsync();

    if (records.Count == 0)
        AnsiConsole.MarkupLine("  [yellow]⚠ No records returned — the report will render empty.[/]");

    // Default the report to the configured year, else the most recent in the data.
    var defaultYear = !string.IsNullOrWhiteSpace(config.MembershipYear)
        ? config.MembershipYear
        : records.Select(r => r.Year)
                 .Where(y => !string.IsNullOrWhiteSpace(y))
                 .OrderByDescending(y => y, StringComparer.Ordinal)
                 .FirstOrDefault();

    var outputPath = ReportGenerator.Write(
        records,
        config.OutputPath,
        environment: HostOf(conn.ConnectedOrgUrl),
        defaultYear: defaultYear,
        statusFilter: "Active");

    // ── Summary ──
    AnsiConsole.WriteLine();
    var activeCount = records.Count(r => r.Status.StartsWith("Active", StringComparison.OrdinalIgnoreCase));
    var t = new Table()
        .Border(TableBorder.Rounded).BorderStyle(Style.Parse("grey"))
        .AddColumn(new TableColumn("[grey]Metric[/]"))
        .AddColumn(new TableColumn("[grey]Value[/]").RightAligned());
    t.AddRow("[white]Records read[/]",   $"[white]{records.Count:N0}[/]");
    t.AddRow("[white]Active[/]",         $"[green]{activeCount:N0}[/]");
    t.AddRow("[white]PTMAs[/]",          $"[white]{records.Select(r => r.Ptma).Distinct().Count():N0}[/]");
    t.AddRow("[white]Default year[/]",   $"[white]{Markup.Escape(defaultYear ?? "—")}[/]");
    t.AddRow("[white]Report[/]",         $"[grey]{Markup.Escape(outputPath)}[/]");
    t.AddRow("[white]Schema[/]",         $"[grey]{Markup.Escape(Path.ChangeExtension(outputPath, null) + ".schema.json")}[/]");
    AnsiConsole.Write(t);

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[green]✓  Report generated[/]").RuleStyle("grey"));

    if (AnsiConsole.Confirm("[grey]Open the report now?[/]", false))
        TryOpen(outputPath);

    return 0;
}
catch (Exception ex)
{
    AnsiConsole.WriteLine();
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    return 1;
}

// ─── helpers ───────────────────────────────────────────────────────────────────
static string HostOf(string url) =>
    Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host : url;

static void TryOpen(string path)
{
    try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
    catch (Exception ex) { AnsiConsole.MarkupLine($"[grey]Could not auto-open: {Markup.Escape(ex.Message)}[/]"); }
}
