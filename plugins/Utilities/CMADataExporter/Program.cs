using CMA.Utilities.DataverseExporter;
using Spectre.Console;

// ─── Banner ───────────────────────────────────────────────────────────────────

AnsiConsole.Clear();

AnsiConsole.Write(
    new FigletText("DV Exporter")
        .Centered()
        .Color(Color.DodgerBlue1));

AnsiConsole.Write(
    new Rule("[grey]Black Ink Development LLC  ·  blackinkbuild.com[/]")
        .RuleStyle(Style.Parse("grey dim"))
        .Centered());

AnsiConsole.WriteLine();

// ─── Load persisted config ────────────────────────────────────────────────────

var config = AppConfig.Load();

// ─── App Mode ─────────────────────────────────────────────────────────────────

var appMode = AnsiConsole.Prompt(
    new SelectionPrompt<AppMode>()
        .Title("[bold white]What would you like to do?[/]")
        .HighlightStyle(Style.Parse("dodgerblue2 bold"))
        .UseConverter(m => m switch
        {
            AppMode.Export => "[dodgerblue2]Export[/]              Export tables to JSON",
            AppMode.Patch => "[gold1]Generate Patches[/]    Generate PATCH payloads from existing export",
            AppMode.ExportAndPatch => "[green]Export + Patches[/]    Export tables to JSON, then generate PATCH payloads",
            _ => m.ToString()
        })
        .AddChoices(AppMode.Export, AppMode.ExportAndPatch, AppMode.Patch));

AnsiConsole.WriteLine();

// ─── Inputs ───────────────────────────────────────────────────────────────────

ExportMode exportMode = ExportMode.Simple;

bool needsConnection = appMode is AppMode.Export or AppMode.ExportAndPatch;
bool needsPatch = appMode is AppMode.Patch or AppMode.ExportAndPatch;

if (needsConnection)
{
    // Dataverse URL — pre-filled from config
    config.DataverseUrl = AnsiConsole
        .Ask("[dodgerblue2]Dataverse URL[/]:",
             string.IsNullOrWhiteSpace(config.DataverseUrl)
                 ? "https://yourorg.crm3.dynamics.com"
                 : config.DataverseUrl)
        .TrimEnd('/');

    exportMode = AnsiConsole.Prompt(
        new SelectionPrompt<ExportMode>()
            .Title("[bold white]Export format?[/]")
            .HighlightStyle(Style.Parse("dodgerblue2 bold"))
            .UseConverter(m => m switch
            {
                ExportMode.Simple => "[white]Simple[/]    [grey]Flat fields — best for PATCH generation[/]",
                ExportMode.Verbose => "[white]Verbose[/]   [grey]Rich types (lookups as objects, optionsets with labels)[/]",
                _ => m.ToString()
            })
            .AddChoices(ExportMode.Simple, ExportMode.Verbose));

    // Export dir — pre-filled from config
    var defaultExportDir = string.IsNullOrWhiteSpace(config.ExportDir)
        ? Path.Combine(Directory.GetCurrentDirectory(), "export")
        : config.ExportDir;

    config.ExportDir = AnsiConsole.Ask("[dodgerblue2]Export output folder[/]:", defaultExportDir);
    Directory.CreateDirectory(config.ExportDir);
}
else
{
    // Patch-only mode still needs to know where the export files are
    var defaultExportDir = string.IsNullOrWhiteSpace(config.ExportDir)
        ? string.Empty
        : config.ExportDir;

    config.ExportDir = AnsiConsole.Ask<string>(
        "[dodgerblue2]Export folder[/] [grey](folder containing *.json export files)[/]:",
        defaultExportDir);
}

if (needsPatch)
{
    var defaultPatchDir = string.IsNullOrWhiteSpace(config.PatchDir)
        ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(config.ExportDir)) ?? config.ExportDir, "patches")
        : config.PatchDir;

    config.PatchDir = AnsiConsole.Ask("[gold1]PATCH output folder[/]:", defaultPatchDir);
    Directory.CreateDirectory(config.PatchDir);
}

// ─── Save config now so prompts are remembered for next run ───────────────────

config.Save();

AnsiConsole.WriteLine();

// ─── Export ───────────────────────────────────────────────────────────────────

if (needsConnection)
{
    AnsiConsole.Write(
        new Rule($"[dodgerblue2]Exporting[/]  [white]{config.DataverseUrl}[/]")
            .RuleStyle("grey"));
    AnsiConsole.WriteLine();

    var exporter = new Exporter(config.DataverseUrl, config.ExportDir);

    try
    {
        await exporter.RunAsync(exportMode);
    }
    catch (Exception ex)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        return 1;
    }
}

// ─── Generate PATCH payloads ──────────────────────────────────────────────────

if (needsPatch)
{
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[gold1]Generating PATCH payloads[/]").RuleStyle("grey"));
    AnsiConsole.WriteLine();

    var generator = new PatchGenerator(config.ExportDir, config.PatchDir);

    try
    {
        await generator.RunAsync();
    }
    catch (Exception ex)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        return 1;
    }
}

AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[green]✓  Complete[/]").RuleStyle("grey"));
AnsiConsole.WriteLine();

return 0;

// ─── Types ────────────────────────────────────────────────────────────────────

public enum AppMode { Export, Patch, ExportAndPatch }