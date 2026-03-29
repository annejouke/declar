using Declar.Cli;
using Declar.Cli.Commands;
using Declar.Cli.Flags;

var options = ArgumentParser.Parse(args);
if (!options.IsValid)
{
	new TerminalReporter().Report(
		Declar.Core.StatementStatus.Error,
		"cli",
		"parse",
		"input",
		options.ErrorMessage ?? "invalid-input");
	return 1;
}

var handler = new CommandHandler()
	.AddBuiltInCommands()
	.AddBuiltInFlags();

var exitCode = await handler.HandleAsync(
	options.Command!,
	options.Declaration!,
	options.Inputs!,
	options.Flags!);

return exitCode;
