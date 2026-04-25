using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TabFlow.Shared.Infrastructure.Diagnostics;
using Xunit;

namespace Shared.Tests.Infrastructure.Diagnostics;

/// <summary>
/// Contract tests for <see cref="HealthJsonWriter"/>. The IETF
/// <c>health+json</c> shape is a downstream contract consumed by load
/// balancers and the release gate; breaking it silently would route
/// traffic to a broken process.
///
/// Unit tier per /doc/docs/explanation/concepts/test-taxonomy.md:
/// the suite touches no file system, network, or database; the
/// only dependency is an in-memory <see cref="DefaultHttpContext"/>
/// with a <see cref="MemoryStream"/> response body.
/// </summary>
[Trait("Category", "Unit")]
public sealed class HealthJsonWriterTests
{
    [Fact]
    public async Task Write_HealthyReport_RendersPassStatus()
    {
        var json = await RenderAsync(HealthStatus.Healthy, NoEntries());

        json.GetProperty("status").GetString().Should().Be("pass");
    }

    [Fact]
    public async Task Write_DegradedReport_RendersWarnStatus()
    {
        var json = await RenderAsync(HealthStatus.Degraded, NoEntries());

        json.GetProperty("status").GetString().Should().Be("warn");
    }

    [Fact]
    public async Task Write_UnhealthyReport_RendersFailStatus()
    {
        var json = await RenderAsync(HealthStatus.Unhealthy, NoEntries());

        json.GetProperty("status").GetString().Should().Be("fail");
    }

    [Fact]
    public async Task Write_SetsContentTypeToHealthJson()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await HealthJsonWriter.Write(
            context,
            new HealthReport(
                new Dictionary<string, HealthReportEntry>(),
                HealthStatus.Healthy,
                TimeSpan.FromMilliseconds(1)));

        context.Response.ContentType.Should().Be("application/health+json");
    }

    [Fact]
    public async Task Write_DbPingProbe_ClassifiesAsDatastore()
    {
        var entry = new HealthReportEntry(
            HealthStatus.Healthy,
            description: "ping ok",
            duration: TimeSpan.FromMilliseconds(4),
            exception: null,
            data: null);

        var json = await RenderAsync(
            HealthStatus.Healthy,
            entries: new Dictionary<string, HealthReportEntry>
            {
                ["platform-db:ping"] = entry,
            });

        json.GetProperty("checks")
            .GetProperty("platform-db:ping")[0]
            .GetProperty("componentType")
            .GetString()
            .Should().Be("datastore");
    }

    [Fact]
    public async Task Write_EventBusProbe_ClassifiesAsSystem()
    {
        var entry = new HealthReportEntry(
            HealthStatus.Healthy,
            description: null,
            duration: TimeSpan.FromMilliseconds(1),
            exception: null,
            data: null);

        var json = await RenderAsync(
            HealthStatus.Healthy,
            entries: new Dictionary<string, HealthReportEntry>
            {
                ["event-bus:capacity"] = entry,
            });

        json.GetProperty("checks")
            .GetProperty("event-bus:capacity")[0]
            .GetProperty("componentType")
            .GetString()
            .Should().Be("system");
    }

    [Fact]
    public async Task Write_PreservesObservedDuration()
    {
        var entry = new HealthReportEntry(
            HealthStatus.Healthy,
            description: null,
            duration: TimeSpan.FromMilliseconds(42),
            exception: null,
            data: null);

        var json = await RenderAsync(
            HealthStatus.Healthy,
            entries: new Dictionary<string, HealthReportEntry>
            {
                ["tenant-db:ping"] = entry,
            });

        var check = json.GetProperty("checks")
            .GetProperty("tenant-db:ping")[0];

        check.GetProperty("observedValue").GetInt64().Should().Be(42);
        check.GetProperty("observedUnit").GetString().Should().Be("ms");
    }

    [Fact]
    public async Task Write_AlwaysIncludesVersionAndReleaseId()
    {
        var json = await RenderAsync(HealthStatus.Healthy, NoEntries());

        json.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("releaseId").GetString().Should().NotBeNullOrEmpty();
    }

    private static Dictionary<string, HealthReportEntry> NoEntries() =>
        new Dictionary<string, HealthReportEntry>();

    private static async Task<JsonElement> RenderAsync(
        HealthStatus status,
        IReadOnlyDictionary<string, HealthReportEntry> entries)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var report = new HealthReport(
            entries.ToDictionary(kv => kv.Key, kv => kv.Value),
            status,
            TimeSpan.FromMilliseconds(1));

        await HealthJsonWriter.Write(context, report);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }
}
