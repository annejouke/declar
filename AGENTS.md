# Agent Instructions for Declar

This is a .NET 10 C# project - an idempotent OS state assistant CLI tool.

## Project Structure

```
src/
├── Declar.slnx              # Solution file
├── Declar.Core/             # Core library (vendors, configs, file implementations)
│   └── Declar.Core.csproj
└── Declar.Cli/              # CLI executable
    └── Declar.Cli.csproj
```

## Build Commands

### Build
```bash
dotnet build ./src/Declar.slnx
# Or for a specific project:
dotnet build ./src/Declar.Cli/Declar.Cli.csproj
```

### Run
```bash
dotnet run --project ./src/Declar.Cli/Declar.Cli.csproj
# Or using just:
just run
```

### Clean
```bash
dotnet clean ./src/Declar.slnx
```

### Restore
```bash
dotnet restore ./src/Declar.slnx
```

## Testing

Tests have not been implemented yet. When adding tests:
- Create a test project using `dotnet new xunit` or `dotnet new nunit`
- Use `dotnet test` to run all tests
- Run a single test with `--filter`:
  ```bash
  dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
  ```

## Code Style Guidelines

### General
- This is C# targeting .NET 10
- **Implicit usings enabled** - do NOT add explicit `using` statements for: `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Net.Http`, `System.Threading`, `System.Threading.Tasks`
- **Nullable reference types enabled** - always use proper nullable annotations (`string?` not `string` for nullable strings)

### Naming Conventions
- Types and methods: `PascalCase`
- Private fields: `camelCase` (or `_camelCase` if using underscore prefix)
- Parameters and local variables: `camelCase`
- Constants: `PascalCase`
- Namespaces: `PascalCase` (typically `Declar.<Module>.<SubModule>`)

### Type Guidelines
- Prefer interfaces for dependencies (e.g., `IVendor`, `IConfigParser`)
- Use records for immutable data transfer objects
- Use pattern matching over type checking
- Use nullable annotations (`?`) for reference types that can be null

### Error Handling
- Use exceptions for exceptional conditions, not normal flow
- Prefer specific exception types over generic `Exception`
- Consider `Result<T>` pattern for operations that may fail in expected ways
- Always handle or document possible exceptions

### Async/Await
- Use async/await over blocking `Task.Wait()` or `Task.Result`
- Name async methods with `Async` suffix
- Do not use `async void` except for event handlers

### Formatting
- 4 spaces for indentation (standard C# convention)
- Braces on new lines for types and members
- Single line braces allowed for simple properties/indexers
- Maximum line length: ~120 characters (soft limit)
- Order class members: fields, constructors, properties, methods (grouped by visibility)

### Documentation
- XML doc comments (`///`) for public APIs
- Include `<summary>`, `<param>`, `<returns>`, `<exception>` as appropriate
- Keep comments focused on "why", not "what"

### Patterns to Follow
- **Vendor pattern**: Each package manager (apt, pacman, dnf, etc.) gets a vendor implementation
- **Config parsers**: TOML, JSON, YAML, CSV, INI, ENV file parsers
- **File implementations**: Wrapper classes for system files (hosts, systemd units, etc.)
- Use dependency injection via `IServiceProvider` or constructor injection

## Project Conventions

### Vendor Abstraction
Vendors should implement a common interface:
```csharp
public interface IVendor
{
    string Name { get; }
    Task<bool> IsInstalledAsync();
    Task InstallAsync(string package);
    Task<bool> IsInstalledAsync(string package);
}
```

### CLI Structure
The CLI uses a verb-noun pattern:
```bash
declar <vendor> <state> <target>
declar <file-type> <operation> <target>
```

### Configuration
- Global config: `~/.config/declar/config.ini`
- Config flags: `--confirm/-c`, `--dry-run`, `--verbose`

## Important Notes

- Always run `dotnet build` after making changes to verify compilation
- Check `dotnet format` if available for code formatting
- The project is in early development - architecture may evolve
- Consider adding unit tests for new vendor implementations
