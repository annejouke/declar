namespace Declar.Cli.Commands;

public static class CommandHandlerCommandExtensions
{
    public static CommandHandler AddBuiltInCommands(this CommandHandler handler)
    {
        return handler
            .AddCommand(new AptCommand())
            .AddCommand(new BrewCommand())
            .AddCommand(new DnfCommand())
            .AddCommand(new FlathubCommand())
            .AddCommand(new HostsCommand())
            .AddCommand(new PacmanCommand())
            .AddCommand(new SnapCommand())
            .AddCommand(new VendorCommand())
            .AddCommand(new WingetCommand());
    }
}
