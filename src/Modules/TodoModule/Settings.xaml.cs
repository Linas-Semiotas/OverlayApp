namespace OverlayApp.Modules.Todo
{
    using System.Text.Json.Nodes;

    public partial class Settings : System.Windows.Controls.UserControl
    {
        private JsonObject _state = new();

        public Settings() { InitializeComponent(); }

        public void Set(JsonObject settings)
        {
            _state = settings ?? new JsonObject();
            ShowCompletedBox.IsChecked = _state["showCompleted"]?.GetValue<bool>() ?? true;
            DoneBottomBox.IsChecked = _state["doneAtBottom"]?.GetValue<bool>() ?? true;
            ItemFontSizeSlider.Value = _state["itemFontSize"]?.GetValue<double>() ?? 16;
            OpacitySlider.Value = _state["backgroundOpacity"]?.GetValue<double>() ?? 0.85;
            WrapLongItemsBox.IsChecked = _state["wrapLongItems"]?.GetValue<bool>() ?? true;
        }

        public JsonObject BuildPayload()
        {
            _state["showCompleted"] = ShowCompletedBox.IsChecked == true;
            _state["doneAtBottom"] = DoneBottomBox.IsChecked == true;
            _state["itemFontSize"] = ItemFontSizeSlider.Value;
            _state["backgroundOpacity"] = OpacitySlider.Value;
            _state["wrapLongItems"] = WrapLongItemsBox.IsChecked == true;
            return _state;
        }
    }
}