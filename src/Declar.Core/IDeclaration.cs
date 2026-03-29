namespace Declar.Core;

public interface IDeclaration
{
    string Name { get; }

    Task<int> ExecuteAsync(CommandContext context);
}
