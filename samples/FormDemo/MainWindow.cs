using System.Runtime.CompilerServices;
using Lumi;
using Lumi.Core;
using Lumi.Core.Components;

namespace FormDemo;

public class MainWindow : Window
{
    // Component references for reading values
    private LumiTextBox? _firstName;
    private LumiTextBox? _lastName;
    private LumiTextBox? _email;
    private LumiTextBox? _password;
    private LumiDropdown? _country;
    private LumiRadioGroup? _role;
    private LumiSlider? _experience;
    private LumiToggle? _newsletter;
    private LumiCheckbox? _termsCheckbox;
    private LumiToggle? _darkMode;
    private LumiDialog? _dialog;
    private Element? _validationArea;
    private Element? _currentSuccessDialog;

    public MainWindow()
    {
        Title = "Lumi — Form Demo";
        Width = 800;
        Height = 750;

        var outputDir = AppContext.BaseDirectory;
        var sourceDir = GetSourceDirectory();

        LoadTemplate(Path.Combine(outputDir, "MainWindow.html"));
        LoadStyleSheet(Path.Combine(outputDir, "MainWindow.css"));

        HtmlPath = Path.Combine(sourceDir, "MainWindow.html");
        CssPath = Path.Combine(sourceDir, "MainWindow.css");
        EnableHotReload = true;
    }

    public override void OnReady()
    {
        _validationArea = FindById("validation-area");

        BuildTextFields();
        BuildSelectionFields();
        BuildToggles();
        BuildActions();
        SetupDialog();
    }

    private void BuildTextFields()
    {
        // First name
        _firstName = new LumiTextBox { Label = "First Name", Placeholder = "John" };
        LumiTooltip.Attach(_firstName.Root, "Enter your first name");
        FindById("field-firstname")?.AddChild(_firstName.Root);

        // Last name
        _lastName = new LumiTextBox { Label = "Last Name", Placeholder = "Doe" };
        LumiTooltip.Attach(_lastName.Root, "Enter your family name");
        FindById("field-lastname")?.AddChild(_lastName.Root);

        // Email
        _email = new LumiTextBox { Label = "Email", Placeholder = "john@example.com" };
        LumiTooltip.Attach(_email.Root, "We'll never share your email");
        FindById("field-email")?.AddChild(_email.Root);

        // Password
        _password = new LumiTextBox { Label = "Password", Placeholder = "••••••••" };
        LumiTooltip.Attach(_password.Root, "Minimum 8 characters");
        FindById("field-password")?.AddChild(_password.Root);
    }

    private void BuildSelectionFields()
    {
        // Country dropdown
        _country = new LumiDropdown
        {
            Items = new List<string>
            {
                "United States", "United Kingdom", "Canada",
                "Germany", "France", "Japan", "Australia"
            },
            SelectedIndex = 0
        };
        FindById("field-country")?.AddChild(_country.Root);

        // Role radio group
        _role = new LumiRadioGroup(new List<string>
        {
            "Developer", "Designer", "Manager", "Other"
        });
        _role.SelectedIndex = 0;
        FindById("field-role")?.AddChild(_role.Root);

        // Experience slider
        _experience = new LumiSlider { Min = 0, Max = 20, Value = 3 };
        var sliderLabel = new TextElement("Years of Experience: 3");
        sliderLabel.InlineStyle = "color: #94A3B8; font-size: 13px; margin: 0px 0px 4px 0px";
        _experience.OnValueChanged = (val) =>
        {
            sliderLabel.Text = $"Years of Experience: {(int)val}";
            sliderLabel.MarkDirty();
        };
        var sliderHost = FindById("field-experience");
        sliderHost?.AddChild(sliderLabel);
        sliderHost?.AddChild(_experience.Root);
    }

    private void BuildToggles()
    {
        var host = FindById("toggles-host");
        if (host == null) return;

        // Newsletter toggle
        _newsletter = new LumiToggle { Label = "Receive newsletter", IsOn = true };
        host.AddChild(_newsletter.Root);

        // Dark mode toggle
        _darkMode = new LumiToggle { Label = "Dark mode", IsOn = true };
        host.AddChild(_darkMode.Root);

        // Terms checkbox
        _termsCheckbox = new LumiCheckbox { Label = "I agree to the Terms of Service" };
        LumiTooltip.Attach(_termsCheckbox.Root, "You must accept the terms to continue");
        host.AddChild(_termsCheckbox.Root);
    }

    private void BuildActions()
    {
        var host = FindById("actions-host");
        if (host == null) return;

        var submitBtn = new LumiButton { Text = "Submit", Variant = ButtonVariant.Primary };
        submitBtn.OnClick = OnSubmit;
        LumiTooltip.Attach(submitBtn.Root, "Submit your registration");
        host.AddChild(submitBtn.Root);

        var resetBtn = new LumiButton { Text = "Reset", Variant = ButtonVariant.Secondary };
        resetBtn.OnClick = OnReset;
        host.AddChild(resetBtn.Root);

        var dangerBtn = new LumiButton { Text = "Cancel", Variant = ButtonVariant.Danger };
        dangerBtn.OnClick = () =>
        {
            if (_dialog != null)
            {
                _dialog.IsOpen = true;
            }
        };
        host.AddChild(dangerBtn.Root);
    }

    private void SetupDialog()
    {
        _dialog = new LumiDialog { Title = "Discard Changes?" };
        var msg = new TextElement("Are you sure you want to cancel? All entered data will be lost.");
        msg.InlineStyle = "color: #94A3B8; font-size: 14px";
        _dialog.Content = msg;
        _dialog.OnClose = () => _dialog.IsOpen = false;
        Root.AddChild(_dialog.Root);
    }

    private void OnSubmit()
    {
        ClearValidation();
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(_firstName?.Value))
            errors.Add("First name is required.");
        if (string.IsNullOrWhiteSpace(_lastName?.Value))
            errors.Add("Last name is required.");
        if (string.IsNullOrWhiteSpace(_email?.Value) || !(_email?.Value.Contains('@') ?? false))
            errors.Add("Please enter a valid email address.");
        if ((_password?.Value.Length ?? 0) < 8)
            errors.Add("Password must be at least 8 characters.");
        if (_termsCheckbox != null && !_termsCheckbox.IsChecked)
            errors.Add("You must agree to the Terms of Service.");

        if (errors.Count > 0)
        {
            foreach (var err in errors)
                AddValidationMessage(err, false);
        }
        else
        {
            AddValidationMessage("Registration successful! Welcome aboard.", true);

            // Show success dialog
            var successDialog = new LumiDialog { Title = "Success! 🎉" };
            var content = new BoxElement("div");
            content.InlineStyle = "display: flex; flex-direction: column; gap: 8px";

            var line1 = new TextElement($"Welcome, {_firstName!.Value} {_lastName!.Value}!");
            line1.InlineStyle = "color: #F8FAFC; font-size: 16px; font-weight: 600";
            content.AddChild(line1);

            var line2 = new TextElement($"Email: {_email!.Value}");
            line2.InlineStyle = "color: #94A3B8; font-size: 14px";
            content.AddChild(line2);

            var roles = new[] { "Developer", "Designer", "Manager", "Other" };
            var selectedRole = _role != null && _role.SelectedIndex >= 0 && _role.SelectedIndex < roles.Length
                ? roles[_role.SelectedIndex] : "Unknown";
            var line3 = new TextElement($"Role: {selectedRole} · Experience: {(int)(_experience?.Value ?? 0)} years");
            line3.InlineStyle = "color: #94A3B8; font-size: 14px";
            content.AddChild(line3);

            successDialog.Content = content;
            successDialog.OnClose = () => successDialog.IsOpen = false;
            successDialog.IsOpen = true;

            if (_currentSuccessDialog != null)
                Root.RemoveChild(_currentSuccessDialog);
            _currentSuccessDialog = successDialog.Root;
            Root.AddChild(_currentSuccessDialog);
        }
    }

    private void OnReset()
    {
        if (_firstName != null) _firstName.Value = "";
        if (_lastName != null) _lastName.Value = "";
        if (_email != null) _email.Value = "";
        if (_password != null) _password.Value = "";
        if (_country != null) _country.SelectedIndex = 0;
        if (_role != null) _role.SelectedIndex = 0;
        if (_experience != null) _experience.Value = 3;
        if (_newsletter != null) _newsletter.IsOn = true;
        if (_darkMode != null) _darkMode.IsOn = true;
        if (_termsCheckbox != null) _termsCheckbox.IsChecked = false;

        ClearValidation();
    }

    private void AddValidationMessage(string message, bool isSuccess)
    {
        if (_validationArea == null) return;

        var msg = new TextElement(message);
        msg.Classes.Add("validation-msg");
        msg.Classes.Add(isSuccess ? "validation-success" : "validation-error");
        _validationArea.AddChild(msg);
    }

    private void ClearValidation()
    {
        if (_validationArea == null) return;
        foreach (var child in _validationArea.Children)
            DisposeElementTree(child);
        _validationArea.ClearChildren();
    }

    private static void DisposeElementTree(Element element)
    {
        foreach (var child in element.Children)
            DisposeElementTree(child);
        element.RemoveAllEventHandlers();
    }

    private static string GetSourceDirectory([CallerFilePath] string callerPath = "")
        => Path.GetDirectoryName(callerPath)!;
}
