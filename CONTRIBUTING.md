# Contributing to Lumi

Thank you for your interest in contributing to Lumi — a .NET 10 native C# GUI framework using HTML/CSS authoring, rendered via SkiaSharp + SDL3.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Clone and Build

```bash
git clone https://github.com/<your-fork>/lumi.git
cd lumi
dotnet build Lumi.slnx
```

### Run Tests

```bash
dotnet test tests/Lumi.Tests/Lumi.Tests.csproj
```

All 951 unit and integration tests should pass before submitting a pull request.

## Project Structure

| Directory | Description |
|---|---|
| `src/Lumi` | Umbrella library — Window, LumiApp, HotReload, Inspector |
| `src/Lumi.Core` | Element tree, components, events, animation, binding |
| `src/Lumi.Styling` | CSS parser, selectors, cascade, HTML templates |
| `src/Lumi.Layout` | Yoga-based flexbox + CSS Grid layout |
| `src/Lumi.Rendering` | SkiaSharp rendering pipeline |
| `src/Lumi.Input` | Hit testing, interaction state |
| `src/Lumi.Platform` | SDL3 windowing |
| `src/Lumi.Text` | HarfBuzz text shaping |
| `src/Lumi.Generators` | Source generator for `[Observable]` |
| `samples/` | 5 sample apps |
| `tests/Lumi.Tests` | 951 unit + integration tests |

## Running Samples

```bash
dotnet run --project samples/HelloWorld
```

Replace `HelloWorld` with any sample project name under `samples/`.

## Code Style

- **Implicit usings** are enabled — avoid unnecessary `using` directives.
- **Nullable reference types** are enabled — annotate nullability correctly.
- **XML doc comments** are recommended on public APIs.
- Follow existing patterns and conventions in the codebase.

## Pull Request Process

1. **Fork** the repository and create a feature branch from `main`.
2. **Make your changes** — keep commits focused and well-described.
3. **Ensure the build passes**: `dotnet build Lumi.slnx`
4. **Ensure all tests pass**: `dotnet test tests/Lumi.Tests/Lumi.Tests.csproj`
5. **Submit a pull request** with a clear description of the change and its motivation.

## Reporting Issues

Found a bug or have a feature request? Please open an issue on [GitHub Issues](../../issues) with:

- A clear, descriptive title.
- Steps to reproduce (for bugs).
- Expected vs. actual behavior.
- .NET version and OS information.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).


## Public API changes

When you add or change a public API, the `Microsoft.CodeAnalysis.PublicApiAnalyzers` analyzer will fail the build with `RS0016` (undeclared API) or `RS0017` (removed API). Add the new line(s) to `PublicAPI.Unshipped.txt` for the affected project (the build error message and the IDE code fix tell you the exact line). On release, entries move from `PublicAPI.Unshipped.txt` into `PublicAPI.Shipped.txt`.
