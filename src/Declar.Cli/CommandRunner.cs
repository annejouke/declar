using System.Diagnostics;
using System.ComponentModel;
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
        if (!options.Report || string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        Console.WriteLine($"     {description.Trim()}");
    }

    public async Task<CommandResult> RunAsync(string fileName, params string[] args)
    {
        DescribeAction($"run: {FormatCommand(fileName, args)}");

        if (options.Test)
        {
            DescribeAction("skipped execution because --test is enabled");
            return new CommandResult(0, string.Empty, string.Empty, true);
        }

        var result = await ExecuteProcessAsync(fileName, args);
        DescribeAction($"run exit code: {result.ExitCode}");
        return result;
    }

    public async Task<CommandResult> RunProbeAsync(string fileName, params string[] args)
    {
        DescribeAction($"probe: {FormatCommand(fileName, args)}");
        var result = await ExecuteProcessAsync(fileName, args);
        DescribeAction($"probe exit code: {result.ExitCode}");
        return result;
    }

    private static async Task<CommandResult> ExecuteProcessAsync(string fileName, IEnumerable<string> args)
    {
        try
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
        catch (Win32Exception ex)
        {
            return new CommandResult(127, string.Empty, ex.Message, false);
        }
    }

    private static string FormatCommand(string fileName, IEnumerable<string> args)
    {
        return string.Join(" ", [fileName, .. args.Select(QuoteArgIfNeeded)]);
    }

    private static string QuoteArgIfNeeded(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg) || arg.Contains(' ', StringComparison.Ordinal))
        {
            return $"\"{arg.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
        }

        return arg;
    }
}
