using System.Runtime.CompilerServices;
using Lumi;

namespace LumiApp.1;

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "LumiApp.1";
        Width = 960;
        Height = 680;

        var dir = AppContext.BaseDirectory;
        LoadTemplate(Path.Combine(dir, "MainWindow.html"));
        LoadStyleSheet(Path.Combine(dir, "MainWindow.css"));
        
        // Hot reload watches the SOURCE files you edit, not the bin/ copies
        var sourceDir = GetSourceDirectory();
        HtmlPath = Path.Combine(sourceDir, "MainWindow.html");
        CssPath = Path.Combine(sourceDir, "MainWindow.css");
        EnableHotReload = true;
    }
    
    private static string GetSourceDirectory([CallerFilePath] string callerPath = "")
        => Path.GetDirectoryName(callerPath)!;
}
