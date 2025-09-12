namespace OverlayApp.Controls
{
    public partial class MyMessageBox : UserControl
    {
        public Action<string?>? OnResult;

        public enum MessageBoxType { Info, Success, Error }
        public enum MessageBoxStyle { Standard, Custom }

        public MyMessageBox(string message, MessageBoxType type, MessageBoxStyle style, params string[] buttons)
        {
            InitializeComponent();
            MessageText.Text = message;

            // Choose accent brush based on type
            var accentKey = type switch
            {
                MessageBoxType.Info => "AccentInfo",
                MessageBoxType.Success => "AccentSuccess",
                MessageBoxType.Error => "AccentError",
                _ => "AccentInfo"
            };

            // Choose base style
            var styleKey = style switch
            {
                MessageBoxStyle.Standard => "MessageBoxStandard",
                MessageBoxStyle.Custom => "MessageBoxCustom",
                _ => "MessageBoxStandard"
            };

            // Apply box style
            ContainerBorder.Style = (Style)FindResource(styleKey);

            // Assign accent to local brush
            Resources["AccentBrush"] = (Brush)Application.Current.Resources[accentKey];

            // Create buttons
            foreach (var btnLabel in buttons)
            {
                var button = new Button
                {
                    Content = btnLabel,
                    Style = (Style)FindResource("DialogButton"),
                    Margin = new Thickness(5, 0, 0, 0),
                    MinWidth = 80,
                    Padding = new Thickness(8, 4, 8, 4)
                };

                button.Click += (_, __) =>
                {
                    OnResult?.Invoke(btnLabel);
                    if (Parent is Panel parent)
                        parent.Children.Remove(this);
                };

                ButtonPanel.Children.Add(button);
            }
        }

        public static MyMessageBox CreateStandard(string message, MessageBoxType type, MessageBoxButton layout)
        {
            return layout switch
            {
                MessageBoxButton.OK => new MyMessageBox(message, type, MessageBoxStyle.Standard, "OK"),
                MessageBoxButton.OKCancel => new MyMessageBox(message, type, MessageBoxStyle.Standard, "OK", "Cancel"),
                MessageBoxButton.YesNo => new MyMessageBox(message, type, MessageBoxStyle.Standard, "Yes", "No"),
                MessageBoxButton.YesNoCancel => new MyMessageBox(message, type, MessageBoxStyle.Standard, "Yes", "No", "Cancel"),
                _ => throw new ArgumentException("Unsupported layout", nameof(layout))
            };
        }

        public static MyMessageBox CreatePrompt(string message,
                                                MessageBoxType type,
                                                string okLabel = "OK",
                                                string cancelLabel = "Cancel",
                                                string? defaultText = null,
                                                MessageBoxStyle style = MessageBoxStyle.Standard)
        {
            var box = new MyMessageBox(message, type, style);

            var promptStyle = box.TryFindResource("MessageBoxPrompt") as Style;
            if (promptStyle != null)
                box.ContainerBorder.Style = promptStyle;

            box.InputPanel.Visibility = Visibility.Visible;
            box.InputBox.Text = defaultText ?? string.Empty;

            box.InputBox.Loaded += (_, __) =>
            {
                box.InputBox.Focus();
                box.InputBox.SelectAll();
            };

            box.InputBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter) { e.Handled = true; box.OnResult?.Invoke(box.InputBox.Text); }
                else if (e.Key == Key.Escape) { e.Handled = true; box.OnResult?.Invoke(null); }
            };

            var okBtn = new Button
            {
                Content = okLabel,
                Style = (Style)box.FindResource("DialogButton"),
                Margin = new Thickness(5, 0, 0, 0),
                MinWidth = 80,
                Padding = new Thickness(8, 4, 8, 4)
            };
            okBtn.Click += (_, __) =>
            {
                box.OnResult?.Invoke(box.InputBox.Text);
                if (box.Parent is Panel parent)
                    parent.Children.Remove(box);
            };
            box.ButtonPanel.Children.Add(okBtn);

            var cancelBtn = new Button
            {
                Content = cancelLabel,
                Style = (Style)box.FindResource("DialogButton"),
                Margin = new Thickness(5, 0, 0, 0),
                MinWidth = 80,
                Padding = new Thickness(8, 4, 8, 4)
            };
            cancelBtn.Click += (_, __) =>
            {
                box.OnResult?.Invoke(null);
                if (box.Parent is Panel parent)
                    parent.Children.Remove(box);
            };
            box.ButtonPanel.Children.Add(cancelBtn);

            return box;
        }
    }
}
