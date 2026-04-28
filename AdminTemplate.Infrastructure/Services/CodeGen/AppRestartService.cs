using System.Diagnostics;
using AdminTemplate.Application.Interfaces.CodeGen;

namespace AdminTemplate.Infrastructure.Services.CodeGen;

public class AppRestartService : IAppRestartService
{
    public async Task BuildAndRestartAsync(string projectRootPath)
    {
        var webProjectPath = Path.Combine(projectRootPath, "AdminTemplate.Web");

        var psi = new ProcessStartInfo("dotnet", $"build \"{webProjectPath}\" --no-restore -v quiet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet build process.");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"dotnet build failed: {error}");
        }

        // Touch a .cshtml to trigger Razor runtime compilation reload
        var sentinelPath = Path.Combine(webProjectPath, "Views", "Shared", "_Layout.cshtml");
        if (File.Exists(sentinelPath))
            File.SetLastWriteTimeUtc(sentinelPath, DateTime.UtcNow);
    }
}
