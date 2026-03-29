namespace Declar.Core;

public interface ITerminalReporter
{
    void Report(StatementStatus status, string command, string declaration, string state, string? reason = null);
}
