using Declar.Core;

namespace Declar.Cli;

public sealed class CommandHandler
{
    private readonly List<ICommand> commands = [];
    private readonly List<IFlag> flags = [];

    public CommandHandler AddCommand(ICommand command)
    {
        commands.Add(command);
        return this;
    }

    public CommandHandler AddFlag(IFlag flag)
    {
        flags.Add(flag);
        return this;
    }

    public async Task<int> HandleAsync(
        string command,
        string declaration,
        IReadOnlyList<string> inputs,
        IReadOnlyList<FlagToken> providedFlags)
    {
        ITerminalReporter reporter = new TerminalReporter();
        var normalizedCommand = command.Trim().ToLowerInvariant();
        var normalizedDeclaration = declaration.Trim().ToLowerInvariant();
        var normalizedInputs = inputs
            .Select(input => input.Trim())
            .Where(input => !string.IsNullOrWhiteSpace(input))
            .ToArray();

        if (normalizedInputs.Length == 0)
        {
            reporter.Report(
                StatementStatus.Error,
                normalizedCommand,
                normalizedDeclaration,
                "(empty)",
                "At least one non-empty input is required.");
            return 1;
        }

        var options = new ExecutionOptions(Report: false, Test: false);
        foreach (var providedFlag in providedFlags)
        {
            var flagHandler = flags.FirstOrDefault(candidate => candidate.CanHandle(providedFlag));
            if (flagHandler is null)
            {
                foreach (var input in normalizedInputs)
                {
                    reporter.Report(
                        StatementStatus.Error,
                        normalizedCommand,
                        normalizedDeclaration,
                        input,
                        $"Unknown flag: {providedFlag.Name}");
                }

                return 1;
            }

            var applyResult = flagHandler.Apply(providedFlag, options);
            if (!applyResult.IsValid)
            {
                foreach (var input in normalizedInputs)
                {
                    reporter.Report(
                        StatementStatus.Error,
                        normalizedCommand,
                        normalizedDeclaration,
                        input,
                        applyResult.ErrorMessage ?? "Invalid flag usage.");
                }

                return 1;
            }

            options = applyResult.Options;
        }

        if (options.Report)
        {
            reporter = new TerminalReporter(useInlineUpdates: false);
        }

        var commandHandler = commands.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, normalizedCommand, StringComparison.Ordinal));
        if (commandHandler is null)
        {
            foreach (var input in normalizedInputs)
            {
                reporter.Report(
                    StatementStatus.Error,
                    normalizedCommand,
                    normalizedDeclaration,
                    input,
                    $"Unsupported command '{normalizedCommand}'.");
            }

            return 1;
        }

        var declarationHandler = commandHandler.Declarations.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, normalizedDeclaration, StringComparison.Ordinal));
        if (declarationHandler is null)
        {
            foreach (var input in normalizedInputs)
            {
                reporter.Report(
                    StatementStatus.Error,
                    normalizedCommand,
                    normalizedDeclaration,
                    input,
                    $"Unsupported declaration '{normalizedDeclaration}' for command '{normalizedCommand}'.");
            }

            return 1;
        }

        var context = new CommandContext(
            normalizedCommand,
            normalizedDeclaration,
            normalizedInputs,
            options,
            new CommandRunner(options),
            reporter);

        return await declarationHandler.ExecuteAsync(context);
    }
}
