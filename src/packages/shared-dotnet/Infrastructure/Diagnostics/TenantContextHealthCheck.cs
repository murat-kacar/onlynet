using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TabFlow.Shared.Infrastructure.Diagnostics;

/// <summary>
/// Health check for the tenant host's per-instance context. A tenant
/// host process is provisioned with a `TABFLOW_TENANT_CODE`
/// environment variable that names the tenant whose database the
/// process owns; the absence of that variable means the host was
/// launched outside its provisioning contract and cannot serve
/// tenant-scoped traffic.
///
/// Registered under the `ready` tag on the tenant host only. Per the
/// spec at
/// <c>/doc/docs/reference/architecture/health-checks.md</c>, a host
/// that lacks its tenant context MUST report unready.
///
/// The richer "is this tenant code active in the platform registry"
/// query is folded into a future iteration when the platform→tenant
/// connection contract is in place; today the probe exercises only
/// the env-variable presence and non-empty content.
///
/// Ledger: closes TD-0013 step 5.
/// </summary>
public sealed class TenantContextHealthCheck : IHealthCheck
{
    public const string EnvVarName = "TABFLOW_TENANT_CODE";

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var tenantCode = Environment.GetEnvironmentVariable(EnvVarName);
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Environment variable {EnvVarName} is not set; tenant host cannot serve traffic without a tenant context."));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Tenant context resolved: {tenantCode}."));
    }
}
