using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TabFlow.Shared.Infrastructure.Diagnostics;

/// <summary>
/// Renders an ASP.NET Core <see cref="HealthReport"/> as the IETF
/// <c>application/health+json</c> response shape defined in
/// <c>draft-inadarei-api-health-check</c>.
///
/// Both hosts share this writer so that <c>/health/live</c> and
/// <c>/health/ready</c> bodies are byte-for-byte equivalent across
/// platform and tenant processes. The contract is documented in
/// <c>/doc/docs/reference/architecture/health-checks.md</c>.
/// </summary>
public static class HealthJsonWriter
{
    private const string ContentType = "application/health+json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static readonly string ReleaseId =
        Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        ?? "unknown";

    private static readonly string Version =
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)
        ?? "0.0.0";

    /// <summary>
    /// Writes the given <paramref name="report"/> to the HTTP response
    /// in IETF <c>health+json</c> shape. The HTTP status code is set
    /// by ASP.NET Core's
    /// <c>HealthCheckOptions.ResultStatusCodes</c> mapping; this method
    /// only writes the body and the content type.
    /// </summary>
    public static Task Write(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = ContentType;

        var payload = new HealthJsonPayload(
            Status: ToIetfStatus(report.Status),
            Version: Version,
            ReleaseId: ReleaseId,
            Checks: report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new[]
                {
                    new HealthJsonCheck(
                        ComponentType: ComponentTypeFor(entry.Key),
                        Status: ToIetfStatus(entry.Value.Status),
                        Time: DateTimeOffset.UtcNow.ToString("O"),
                        ObservedValue: (long)entry.Value.Duration.TotalMilliseconds,
                        ObservedUnit: "ms",
                        Output: entry.Value.Description),
                }));

        return JsonSerializer.SerializeAsync(
            context.Response.Body,
            payload,
            SerializerOptions,
            context.RequestAborted);
    }

    private static string ToIetfStatus(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "pass",
        HealthStatus.Degraded => "warn",
        HealthStatus.Unhealthy => "fail",
        _ => "fail",
    };

    /// <summary>
    /// Best-effort component classification by probe ID convention
    /// (<c>{component}:{probe}</c>). Probe authors are free to override
    /// by naming their checks accordingly. Unknown components default
    /// to <c>"component"</c>.
    /// </summary>
    private static string ComponentTypeFor(string probeId)
    {
        if (probeId.Contains("-db:", StringComparison.Ordinal) ||
            probeId.EndsWith(":ping", StringComparison.Ordinal) ||
            probeId.EndsWith(":migrations", StringComparison.Ordinal))
        {
            return "datastore";
        }

        if (probeId.StartsWith("event-bus", StringComparison.Ordinal) ||
            probeId.Contains(":capacity", StringComparison.Ordinal))
        {
            return "system";
        }

        return "component";
    }

    private sealed record HealthJsonPayload(
        string Status,
        string Version,
        string ReleaseId,
        IReadOnlyDictionary<string, HealthJsonCheck[]> Checks);

    private sealed record HealthJsonCheck(
        string ComponentType,
        string Status,
        string Time,
        long ObservedValue,
        string ObservedUnit,
        string? Output);
}
