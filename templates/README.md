# Lumi.Templates

Project templates for the [Lumi](https://github.com/PanamaP/Lumi) desktop GUI framework.

## Installation

```bash
dotnet new install Lumi.Templates
```

## Usage

Create a new Lumi app:

```bash
dotnet new lumi -n MyApp
cd MyApp
dotnet run
```

This scaffolds a ready-to-run desktop application with:

- **Program.cs** — one-line app entry point
- **MainWindow.cs** — window class with HTML/CSS loading and hot reload
- **MainWindow.html** — UI structure
- **MainWindow.css** — styling

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Included Templates

| Short Name | Description |
|------------|-------------|
| `lumi`     | A minimal Lumi desktop app with an HTML template and CSS stylesheet |

## Uninstall

```bash
dotnet new uninstall Lumi.Templates
```

## Learn More

- [Getting Started Guide](https://github.com/PanamaP/Lumi/blob/main/docs/getting-started.md)
- [Lumi README](https://github.com/PanamaP/Lumi/blob/main/README.md)
