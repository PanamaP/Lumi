using Lumi.Core;
using Lumi.Core.Components;

namespace Lumi.Tests;

public class ComponentTests
{
    // ── LumiButton ──────────────────────────────────────────────────

    [Fact]
    public void Button_CreatesElementTree()
    {
        var btn = new LumiButton { Text = "OK" };

        Assert.NotNull(btn.Root);
        Assert.Single(btn.Root.Children);
        Assert.IsType<TextElement>(btn.Root.Children[0]);
        Assert.Equal("OK", ((TextElement)btn.Root.Children[0]).Text);
    }

    [Fact]
    public void Button_Click_FiresOnClick()
    {
        var btn = new LumiButton { Text = "Go" };
        bool fired = false;
        btn.OnClick = () => fired = true;

        SimulateClick(btn.Root);

        Assert.True(fired);
    }

    [Fact]
    public void Button_Disabled_BlocksClicks()
    {
        var btn = new LumiButton { Text = "No", IsDisabled = true };
        bool fired = false;
        btn.OnClick = () => fired = true;

        SimulateClick(btn.Root);

        Assert.False(fired);
    }

    [Fact]
    public void Button_VariantChangesStyle()
    {
        var btn = new LumiButton { Variant = ButtonVariant.Danger };

        // ComponentStyles now uses InlineStyle; verify the inline style contains danger color
        Assert.Contains("background-color:", btn.Root.InlineStyle);
        Assert.Contains(ComponentStyles.ToRgba(ComponentStyles.Danger), btn.Root.InlineStyle);
    }

    // ── LumiCheckbox ────────────────────────────────────────────────

    [Fact]
    public void Checkbox_Toggle_ChangesIsChecked()
    {
        var cb = new LumiCheckbox { Label = "Accept" };
        Assert.False(cb.IsChecked);

        SimulateClick(cb.Root);

        Assert.True(cb.IsChecked);
    }

    [Fact]
    public void Checkbox_Toggle_FiresOnChanged()
    {
        var cb = new LumiCheckbox();
        bool? received = null;
        cb.OnChanged = v => received = v;

        SimulateClick(cb.Root);

        Assert.True(received);
    }

    [Fact]
    public void Checkbox_DoubleToggle_ReturnsFalse()
    {
        var cb = new LumiCheckbox();

        SimulateClick(cb.Root);
        SimulateClick(cb.Root);

        Assert.False(cb.IsChecked);
    }

    // ── LumiSlider ──────────────────────────────────────────────────

    [Fact]
    public void Slider_ValueClampsToMinMax()
    {
        var slider = new LumiSlider { Min = 0, Max = 100 };

        slider.Value = 150;
        Assert.Equal(100, slider.Value);

        slider.Value = -10;
        Assert.Equal(0, slider.Value);
    }

    [Fact]
    public void Slider_DefaultRange()
    {
        var slider = new LumiSlider();

        Assert.Equal(0f, slider.Min);
        Assert.Equal(1f, slider.Max);
        Assert.Equal(0f, slider.Value);
    }

    [Fact]
    public void Slider_SettingMinClampsValue()
    {
        var slider = new LumiSlider { Min = 0, Max = 100, Value = 10 };

        slider.Min = 50;

        Assert.Equal(50, slider.Value);
    }

    [Fact]
    public void Slider_SettingMaxClampsValue()
    {
        var slider = new LumiSlider { Min = 0, Max = 100, Value = 80 };

        slider.Max = 50;

        Assert.Equal(50, slider.Value);
    }

    // ── LumiDropdown ────────────────────────────────────────────────

    [Fact]
    public void Dropdown_SelectionChangesIndex()
    {
        var dd = new LumiDropdown { Items = ["A", "B", "C"] };

        dd.SelectedIndex = 1;

        Assert.Equal(1, dd.SelectedIndex);
    }

    [Fact]
    public void Dropdown_SelectionFiresEvent()
    {
        var dd = new LumiDropdown { Items = ["X", "Y"] };
        int? received = null;
        dd.OnSelectionChanged = idx => received = idx;

        // Open the dropdown
        SimulateClick(dd.Root.Children[0]); // click button
        Assert.True(dd.IsOpen);

        // Click second item
        var listContainer = dd.Root.Children[1];
        SimulateClick(listContainer.Children[1]);

        Assert.Equal(1, received);
        Assert.Equal(1, dd.SelectedIndex);
    }

    [Fact]
    public void Dropdown_ToggleOpen()
    {
        var dd = new LumiDropdown { Items = ["A"] };

        Assert.False(dd.IsOpen);
        SimulateClick(dd.Root.Children[0]);
        Assert.True(dd.IsOpen);
        SimulateClick(dd.Root.Children[0]);
        Assert.False(dd.IsOpen);
    }

    // ── LumiDialog ──────────────────────────────────────────────────

    [Fact]
    public void Dialog_IsOpenControlsVisibility()
    {
        var dlg = new LumiDialog { Title = "Info" };

        Assert.False(dlg.IsOpen);
        Assert.Contains("display: none", dlg.Root.InlineStyle);

        dlg.IsOpen = true;
        Assert.Contains("display: flex", dlg.Root.InlineStyle);

        dlg.IsOpen = false;
        Assert.Contains("display: none", dlg.Root.InlineStyle);
    }

    [Fact]
    public void Dialog_CloseButtonFiresOnClose()
    {
        var dlg = new LumiDialog { Title = "Test", IsOpen = true };
        bool closed = false;
        dlg.OnClose = () => closed = true;

        // Close button is in titleBar (panel > titleBar > closeButton)
        var panel = dlg.Root.Children[0];     // panel
        var titleBar = panel.Children[0];      // titleBar
        var closeBtn = titleBar.Children[1];   // close button
        SimulateClick(closeBtn);

        Assert.True(closed);
        Assert.False(dlg.IsOpen);
    }

    [Fact]
    public void Dialog_ContentCanBeSet()
    {
        var dlg = new LumiDialog();
        var content = new TextElement("Hello");

        dlg.Content = content;

        var panel = dlg.Root.Children[0];
        var contentArea = panel.Children[1];
        Assert.Single(contentArea.Children);
        Assert.Same(content, contentArea.Children[0]);
    }

    // ── LumiList ────────────────────────────────────────────────────

    [Fact]
    public void List_ItemsCreateCorrectNumberOfChildren()
    {
        var list = new LumiList { Items = ["A", "B", "C", "D"] };

        Assert.Equal(4, list.Root.Children.Count);
    }

    [Fact]
    public void List_ItemClickFiresEvent()
    {
        var list = new LumiList { Items = ["X", "Y", "Z"] };
        int? clicked = null;
        list.OnItemClick = idx => clicked = idx;

        SimulateClick(list.Root.Children[1]);

        Assert.Equal(1, clicked);
    }

    [Fact]
    public void List_EmptyItemsList()
    {
        var list = new LumiList { Items = [] };

        Assert.Empty(list.Root.Children);
    }

    [Fact]
    public void List_RebuildOnNewItems()
    {
        var list = new LumiList { Items = ["A"] };
        Assert.Single(list.Root.Children);

        list.Items = ["A", "B", "C"];
        Assert.Equal(3, list.Root.Children.Count);
    }

    // ── LumiTextBox ─────────────────────────────────────────────────

    [Fact]
    public void TextBox_ValueSyncsWithInputElement()
    {
        var tb = new LumiTextBox();
        tb.Value = "hello";

        Assert.Equal("hello", tb.InputElement.Value);
    }

    [Fact]
    public void TextBox_PlaceholderSyncs()
    {
        var tb = new LumiTextBox { Placeholder = "Enter name..." };

        Assert.Equal("Enter name...", tb.InputElement.Placeholder);
    }

    [Fact]
    public void TextBox_HasLabelAndInput()
    {
        var tb = new LumiTextBox { Label = "Name" };

        // Container should have label + input
        Assert.Equal(2, tb.Root.Children.Count);
        Assert.IsType<TextElement>(tb.Root.Children[0]);
        Assert.IsType<InputElement>(tb.Root.Children[1]);
        Assert.Equal("Name", ((TextElement)tb.Root.Children[0]).Text);
    }

    [Fact]
    public void TextBox_OnValueChangedFires()
    {
        var tb = new LumiTextBox();
        string? received = null;
        tb.OnValueChanged = v => received = v;

        // Simulate input event via dispatcher
        tb.InputElement.Value = "test";
        EventDispatcher.Dispatch(new RoutedEvent("input"), tb.InputElement);

        Assert.Equal("test", received);
    }

    // ── Helper ──────────────────────────────────────────────────────

    private static void SimulateClick(Element target)
    {
        var e = new RoutedMouseEvent("click") { Button = MouseButton.Left };
        EventDispatcher.Dispatch(e, target);
    }
}
