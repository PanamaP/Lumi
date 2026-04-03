using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lumi.Core;
using Lumi.Core.Binding;

namespace StressTest.Scenarios;

/// <summary>
/// 100 data-bound TextElements updating 20 random properties per frame.
/// Measures: binding update overhead.
/// </summary>
public class BindingStress : IStressScenario
{
    public string Name => "Binding Stress";
    public string Description => "100 bound elements, 20 property changes/frame — stress binding engine";

    private StressViewModel? _viewModel;
    private BindingEngine? _bindingEngine;
    private readonly Random _rng = new(42);

    public void Setup(StressWindow window, Element container)
    {
        _viewModel = new StressViewModel();
        _bindingEngine = new BindingEngine();

        for (int i = 0; i < 100; i++)
        {
            var row = new BoxElement("div");
            row.ComputedStyle.Display = DisplayMode.Flex;
            row.ComputedStyle.FlexDirection = FlexDirection.Row;
            row.ComputedStyle.AlignItems = AlignItems.Center;
            row.ComputedStyle.Padding = new EdgeValues(4, 8, 4, 8);
            row.ComputedStyle.Margin = new EdgeValues(1, 0, 1, 0);
            row.ComputedStyle.BackgroundColor = Color.FromHex("#1E293B");
            row.ComputedStyle.BorderRadius = 4;

            var label = new TextElement($"[{i:D3}] ");
            label.ComputedStyle.Color = Color.FromHex("#64748B");
            label.ComputedStyle.FontSize = 12;

            var valueText = new TextElement(_viewModel.Values[i]);
            valueText.ComputedStyle.Color = Color.FromHex("#38BDF8");
            valueText.ComputedStyle.FontSize = 12;

            // Bind text element to corresponding property
            var expr = BindingExpression.Parse($"{{Binding Values[{i}]}}");
            _bindingEngine.Bind(valueText, "Text", _viewModel, expr);

            row.AddChild(label);
            row.AddChild(valueText);
            container.AddChild(row);
        }
    }

    public void Update(int frameNumber)
    {
        if (_viewModel == null || _bindingEngine == null) return;

        // Change 20 random properties per frame
        for (int i = 0; i < 20; i++)
        {
            int index = _rng.Next(100);
            _viewModel.Values[index] = $"Value={_rng.Next(10000):D5} t={frameNumber}";
        }

        _viewModel.NotifyAllChanged();
        _bindingEngine.UpdateAll();
    }
}

public class StressViewModel : INotifyPropertyChanged
{
    public string[] Values { get; } = new string[100];

    public StressViewModel()
    {
        for (int i = 0; i < 100; i++)
        {
            Values[i] = $"Initial-{i:D3}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyAllChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
