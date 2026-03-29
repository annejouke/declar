using System.Diagnostics;

namespace Declar.Cli.Test;

internal static class CliProcessTestHelper
{
    public static async Task<ProcessResult> RunCliAsync(params string[] cliArgs)
    {
        var repositoryRoot = FindRepositoryRoot();

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add("./src/Declar.Cli/Declar.Cli.csproj");
        startInfo.ArgumentList.Add("--");

        foreach (var argument in cliArgs)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new ProcessResult(
            process.ExitCode,
            await stdOutTask,
            await stdErrTask);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var hasSolution = File.Exists(Path.Combine(current.FullName, "src", "Declar.slnx"));
            var hasJustfile = File.Exists(Path.Combine(current.FullName, "justfile"));
            if (hasSolution || hasJustfile)
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test execution directory.");
    }
}

internal sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
