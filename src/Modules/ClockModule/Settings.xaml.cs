namespace OverlayApp.Modules.Clock
{
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Windows.Controls;

    public partial class Settings : System.Windows.Controls.UserControl
    {
        private JsonObject _state = new();

        public Settings()
        {
            InitializeComponent();
        }

        public void Set(JsonObject settings)
        {
            _state = settings ?? new JsonObject();

            SetCombo(TimeFormatBox, _state["timeFormat"]?.GetValue<string>() ?? "HH:mm:ss");
            SetCombo(DateFormatBox, _state["dateFormat"]?.GetValue<string>() ?? "yyyy-MM-dd");

            ShowDateBox.IsChecked = _state["showDate"]?.GetValue<bool>() ?? true;
            ShowWeekdayBox.IsChecked = _state["showWeekday"]?.GetValue<bool>() ?? false;

            TimeScaleSlider.Value = _state["timeScale"]?.GetValue<double>() ?? 1.0;
            PaddingSlider.Value = _state["padding"]?.GetValue<double>() ?? 8;
            OpacitySlider.Value = _state["backgroundOpacity"]?.GetValue<double>() ?? 0.6;
        }

        public JsonObject BuildPayload()
        {
            _state["timeFormat"] = GetCombo(TimeFormatBox);
            _state["dateFormat"] = GetCombo(DateFormatBox);
            _state["showDate"] = ShowDateBox.IsChecked == true;
            _state["showWeekday"] = ShowWeekdayBox.IsChecked == true;
            _state["timeScale"] = TimeScaleSlider.Value;
            _state["padding"] = PaddingSlider.Value;
            _state["backgroundOpacity"] = OpacitySlider.Value;
            return _state;
        }

        private static void SetCombo(ComboBox cb, string value)
        {
            var idx = cb.Items.Cast<ComboBoxItem>()
                              .Select((item, i) => (item, i))
                              .FirstOrDefault(t => t.item.Content?.ToString() == value).i;
            cb.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private static string GetCombo(ComboBox cb) =>
            ((ComboBoxItem)cb.SelectedItem)?.Content?.ToString() ?? "";
    }
}
