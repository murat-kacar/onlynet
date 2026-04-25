using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TabFlow.PlatformWorker;
using TabFlow.Shared.Domain.Enums;
using TabFlow.Shared.Infrastructure.Data;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<PlatformDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PlatformDb")));

builder.Services.AddHostedService<ProvisioningWorker>();

IHost host = builder.Build();

await host.RunAsync();
