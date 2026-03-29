namespace Declar.Core;

public sealed record CommandContext(
    string Command,
    string Declaration,
    IReadOnlyList<string> Inputs,
    ExecutionOptions Options,
    ICommandShell Shell,
    ITerminalReporter Reporter);
