using OverlayApp.Managers;
using OverlayApp.Controls;
using OverlayApp.Helpers;
using static OverlayApp.Controls.MyMessageBox;

namespace OverlayApp.Views
{
    public partial class GeneralSettingsView : UserControl
    {
        public GeneralSettingsView()
        {
            InitializeComponent();
            LoadThemeOptions();
        }

        private void LoadThemeOptions()
        {
            ThemeComboBox.Items.Clear();
            var themes = ThemeManager.GetAvailableThemes();

            foreach (var item in themes)
                ThemeComboBox.Items.Add(item);

            string? currentFile = AppStateManager.Current["theme"]?.ToString();
            if (currentFile != null)
            {
                ThemeComboBox.SelectedItem = ThemeComboBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(x => System.IO.Path.GetFileName((string)x.Tag) == currentFile);
            }

        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
                ThemeManager.ApplyThemeFromComboBoxItem(selectedItem);
        }

        private void ResetSettings(object sender, RoutedEventArgs e)
        {
            AppStateManager.Reset();
            ThemeManager.ApplyThemeFromState();
            MainWindow.Instance.ReloadModules();

            LoadThemeOptions(); // Repopulate and select updated default

            var box = new MyMessageBox(
                "Settings have been reset to defaults.",
                MessageBoxType.Info,
                MessageBoxStyle.Standard,
                "OK");

            MessageBoxHelper.Show(box);
        }
    }
}
