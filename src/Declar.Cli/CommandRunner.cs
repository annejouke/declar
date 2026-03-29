using System.Diagnostics;
using Declar.Core;

namespace Declar.Cli;

public sealed class CommandRunner : ICommandShell
{
    private readonly ExecutionOptions options;

    public CommandRunner(ExecutionOptions options)
    {
        this.options = options;
    }

    public void DescribeAction(string description)
    {
        _ = description;
    }

    public async Task<CommandResult> RunAsync(string fileName, params string[] args)
    {
        if (options.Test)
        {
            return new CommandResult(0, string.Empty, string.Empty, true);
        }

        return await ExecuteProcessAsync(fileName, args);
    }

    public async Task<CommandResult> RunProbeAsync(string fileName, params string[] args)
    {
        return await ExecuteProcessAsync(fileName, args);
    }

    private static async Task<CommandResult> ExecuteProcessAsync(string fileName, IEnumerable<string> args)
    {

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;

        return new CommandResult(process.ExitCode, stdOut, stdErr, false);
    }
}
