using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Spectre.Console;

namespace CMA.Utilities.MembershipReport;

/// <summary>
/// Owns a <see cref="ServiceClient"/> authenticated with an app registration
/// (client id + secret) and keeps it usable across transient network drops.
///
/// Two layers of resilience:
///   • The ServiceClient's own throttling retry (MaxRetryCount / RetryPauseTime).
///   • <see cref="ExecuteAsync{T}"/>, which retries with exponential backoff and,
///     if the client has fallen offline (IsReady == false), transparently
///     rebuilds it before the next attempt — so a dropped connection recovers
///     without failing the run.
/// </summary>
public sealed class DataverseConnection : IDisposable
{
    private readonly AppConfig _config;
    private ServiceClient _client;

    private const int MaxAttempts = 5;               // per operation
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(2); // 2,4,8,16s

    private DataverseConnection(AppConfig config, ServiceClient client)
    {
        _config = config;
        _client = client;
    }

    /// <summary>Connects and verifies the client is ready, retrying the initial connect.</summary>
    public static async Task<DataverseConnection> ConnectAsync(AppConfig config)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var client = Build(config);
                if (client.IsReady)
                    return new DataverseConnection(config, client);

                var reason = client.LastError;
                client.Dispose();
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(reason)
                        ? "Dataverse client did not become ready."
                        : reason);
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                var delay = Backoff(attempt);
                AnsiConsole.MarkupLine(
                    $"  [yellow]⚠[/] [yellow]Connect attempt {attempt} failed:[/] " +
                    $"[dim]{Markup.Escape(ex.Message)}[/] — retrying in {delay.TotalSeconds:N0}s");
                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// Runs a Dataverse operation with retry + reconnect. <paramref name="op"/>
    /// receives a live <see cref="IOrganizationService"/> and must be idempotent
    /// (reads are; this tool only reads).
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<IOrganizationService, T> op, string label)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (!_client.IsReady)
                    Reconnect("client reported not-ready");

                return await Task.Run(() => op(_client));
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex))
            {
                var delay = Backoff(attempt);
                AnsiConsole.MarkupLine(
                    $"  [yellow]⚠[/] [yellow]{Markup.Escape(label)} failed (attempt {attempt}):[/] " +
                    $"[dim]{Markup.Escape(ex.Message)}[/] — retrying in {delay.TotalSeconds:N0}s");
                await Task.Delay(delay);

                // On the way back up, make sure we have a live client.
                if (!_client.IsReady)
                    Reconnect("re-establishing after transient failure");
            }
        }
    }

    public string ConnectedOrgUrl =>
        _client.ConnectedOrgUriActual?.ToString() ?? _config.DataverseUrl;

    // ── internals ──────────────────────────────────────────────────────────────

    private void Reconnect(string why)
    {
        AnsiConsole.MarkupLine($"  [grey]↻ Reconnecting to Dataverse ({Markup.Escape(why)})…[/]");
        try { _client.Dispose(); } catch { /* ignore */ }
        _client = Build(_config);
        if (!_client.IsReady)
            throw new InvalidOperationException(
                $"Reconnect failed. Last error: {_client.LastError}");
    }

    private static ServiceClient Build(AppConfig config)
    {
        // Client-credentials (app registration) auth — no interactive login.
        var connStr =
            "AuthType=ClientSecret;" +
            $"Url={config.DataverseUrl};" +
            $"ClientId={config.ClientId};" +
            $"ClientSecret={config.ClientSecret};" +
            "RequireNewInstance=True";

        var client = new ServiceClient(connStr)
        {
            // Built-in throttling/transient retry on top of our own loop.
            MaxRetryCount = 5,
            RetryPauseTime = TimeSpan.FromSeconds(5)
        };
        return client;
    }

    private static TimeSpan Backoff(int attempt) =>
        TimeSpan.FromSeconds(BaseDelay.TotalSeconds * Math.Pow(2, attempt - 1));

    // Treat connectivity / server-side / throttling errors as retryable; leave
    // genuine request errors (bad query, auth rejected) to surface immediately.
    private static bool IsTransient(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException!)
        {
            switch (e)
            {
                case HttpRequestException:
                case TimeoutException:
                case System.Net.Sockets.SocketException:
                case TaskCanceledException:
                    return true;
            }

            var m = e.Message;
            if (m.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("throttl", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("transient", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("service is unavailable", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("503") || m.Contains("504") || m.Contains("429"))
                return true;

            if (e.InnerException is null) break;
        }
        return false;
    }

    public void Dispose()
    {
        try { _client.Dispose(); } catch { /* ignore */ }
    }
}
