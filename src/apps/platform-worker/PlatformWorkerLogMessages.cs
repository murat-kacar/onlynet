using Microsoft.Extensions.Logging;

namespace TabFlow.PlatformWorker;

/// <summary>
/// Source-generated <see cref="ILogger"/> extension methods for the
/// platform worker. Each call site in this assembly invokes one of
/// these methods instead of the structured-logging
/// <see cref="LoggerExtensions"/> overloads so that the analyser
/// rules CA1848 (LoggerMessage delegates) and CA1873 (evaluation of
/// this argument may be expensive) can be enforced — see TD-0014
/// step 3 for the ratchet plan.
///
/// EventId allocation is per-assembly and starts at 1; the IDs are
/// stable so log search by EventId remains meaningful across builds.
/// </summary>
internal static partial class PlatformWorkerLogMessages
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Error processing provisioning jobs")]
    public static partial void ErrorProcessingProvisioningJobs(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Error claiming/processing job {JobId}")]
    public static partial void ErrorClaimingJob(this ILogger logger, Guid jobId, Exception ex);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Claimed job {JobId} for tenant {TenantId}")]
    public static partial void ClaimedJob(this ILogger logger, Guid jobId, Guid tenantId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Failed to provision tenant {TenantId}")]
    public static partial void FailedToProvisionTenant(this ILogger logger, Guid tenantId, Exception ex);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Completed job {JobId}")]
    public static partial void CompletedJob(this ILogger logger, Guid jobId);
}
