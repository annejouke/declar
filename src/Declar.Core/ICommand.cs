namespace Declar.Core;

public interface ICommand
{
    string Name { get; }

    IReadOnlyList<IDeclaration> Declarations { get; }
}
