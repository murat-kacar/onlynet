using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TabFlow.PlatformWorker;
using TabFlow.Shared.Domain.Enums;
using TabFlow.Shared.Infrastructure.Data;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

// TD-0026: register Systemd lifetime so the worker signals readiness
// to systemd via sd_notify("READY=1") only after the BackgroundService
// has started polling. The reference unit set in
// /doc/docs/how-to/supervise-processes.md uses Type=notify and
// depends on this. AddSystemd() is a no-op when INVOCATION_ID is
// unset (i.e. outside systemd), so it is safe in dotnet run and
// in tests.
builder.Services.AddSystemd();

builder.Services.AddDbContext<PlatformDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PlatformDb")));

builder.Services.AddHostedService<ProvisioningWorker>();

IHost host = builder.Build();

await host.RunAsync();
