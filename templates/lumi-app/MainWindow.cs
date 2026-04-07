using Lumi;

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
    }
}
