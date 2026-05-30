using System.Diagnostics;
using System.Net.Http;
using Xunit;

namespace PlaywrightTests;

[CollectionDefinition("ApiServer", DisableParallelization = true)]
public sealed class ApiServerCollection : ICollectionFixture<ApiServerFixture>
{
}

public sealed class ApiServerFixture : IAsyncLifetime
{
    private const string BaseUrl = "https://localhost:7008";
    private const string StatusEndpoint = "/api/v1/auth/status";
    private Process? _process;
    private bool _ownsProcess;

    public async ValueTask InitializeAsync()
    {
        var password = EnsureDefaultPassword();
        EnsureSeedEnvironment(password);

        if (await IsApiAvailableAsync())
        {
            _ownsProcess = false;
            return;
        }

        StartApiProcess(password);
        _ownsProcess = true;

        var ready = await WaitForApiAsync(TimeSpan.FromSeconds(60));
        if (!ready)
        {
            throw new InvalidOperationException("API failed to start on https://localhost:7008.");
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_ownsProcess && _process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore shutdown failures.
            }
        }

        _process?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static string EnsureDefaultPassword()
    {
        var password = Environment.GetEnvironmentVariable("PLAYWRIGHT_DEFAULT_USER_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            password = "PharmaQMS!1234";
            Environment.SetEnvironmentVariable("PLAYWRIGHT_DEFAULT_USER_PASSWORD", password);
        }

        return password;
    }

    private static void EnsureSeedEnvironment(string password)
    {
        Environment.SetEnvironmentVariable("Seed__EnableDefaultUsers", "true");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Seed__DefaultUserPassword")))
        {
            Environment.SetEnvironmentVariable("Seed__DefaultUserPassword", password);
        }
    }

    private void StartApiProcess(string password)
    {
        var apiProjectPath = ResolveApiProjectPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{apiProjectPath}\" --launch-profile https --no-build",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        startInfo.Environment["Seed__EnableDefaultUsers"] = "true";
        startInfo.Environment["Seed__DefaultUserPassword"] = password;

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start API process.");
        _process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                Console.WriteLine($"[api] {args.Data}");
            }
        };
        _process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                Console.WriteLine($"[api:err] {args.Data}");
            }
        };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private static string ResolveApiProjectPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "PharmaQMS.API", "PharmaQMS.API.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate PharmaQMS.API.csproj.");
    }

    private static async Task<bool> IsApiAvailableAsync()
    {
        try
        {
            using var client = CreateHttpClient();
            using var response = await client.GetAsync(BaseUrl + StatusEndpoint);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> WaitForApiAsync(TimeSpan timeout)
    {
        using var client = CreateHttpClient();
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                using var response = await client.GetAsync(BaseUrl + StatusEndpoint);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Ignore and retry until timeout.
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return false;
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }
}
