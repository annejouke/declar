namespace Declar.Core;

public interface ICommandShell
{
    void DescribeAction(string description);

    Task<CommandResult> RunAsync(string fileName, params string[] args);

    Task<CommandResult> RunProbeAsync(string fileName, params string[] args);
}
