namespace Declar.Core;

public sealed record CommandResult(int ExitCode, string StdOut, string StdErr, bool WasSkipped);
