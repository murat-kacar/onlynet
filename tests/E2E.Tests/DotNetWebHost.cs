using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace TabFlow.E2E.Tests;

internal sealed class DotNetWebHost : IAsyncDisposable
{
    private readonly Process _process;

    private DotNetWebHost(Process process, Uri baseAddress)
    {
        _process = process;
        BaseAddress = baseAddress;
    }

    public Uri BaseAddress { get; }

    public static async Task<DotNetWebHost> StartAsync(string projectRelativePath)
    {
        string root = FindRepositoryRoot();
        int port = ReservePort();
        Uri baseAddress = new($"http://127.0.0.1:{port}");
        string projectPath = Path.Combine(root, projectRelativePath);

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(baseAddress.ToString());

        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Testing";
        startInfo.Environment["ConnectionStrings__PlatformDb"] =
            "Host=127.0.0.1;Port=5432;Database=tabflow_platform_e2e;Username=postgres;Password=postgres";
        startInfo.Environment["ConnectionStrings__TenantDb"] =
            "Host=127.0.0.1;Port=5432;Database=tabflow_tenant_e2e;Username=postgres;Password=postgres";
        startInfo.Environment["TABFLOW_TENANT_CODE"] = "E2E";

        Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the ASP.NET Core host process.");
        DotNetWebHost host = new(process, baseAddress);

        try
        {
            await host.WaitUntilReadyAsync();
            return host;
        }
        catch
        {
            await host.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
        }

        await _process.WaitForExitAsync();
        _process.Dispose();
    }

    private async Task WaitUntilReadyAsync()
    {
        using HttpClient client = new() { BaseAddress = BaseAddress };
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        Exception? lastFailure = null;

        while (!timeout.IsCancellationRequested)
        {
            if (_process.HasExited)
            {
                string output = await _process.StandardOutput.ReadToEndAsync();
                string error = await _process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException(
                    $"Host process exited before becoming ready.{Environment.NewLine}{output}{Environment.NewLine}{error}");
            }

            try
            {
                using HttpResponseMessage response = await client.GetAsync("/login", timeout.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastFailure = ex;
            }

            await Task.Delay(250, CancellationToken.None);
        }

        throw new TimeoutException($"Host at {BaseAddress} did not become ready.", lastFailure);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "TabFlow.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate repository root from the test output directory.");
    }

    private static int ReservePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
