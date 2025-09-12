using OverlayApp.ModuleSystem;
using OverlayApp.Views;

namespace OverlayApp.Managers
{
    public static class ModeManager
    {
        private static MainWindow? _mainWindow;
        public static bool IsEditMode { get; private set; }
        public static bool IsManagerMode { get; private set; }
        public static bool IsSettingsMode { get; private set; }

        public static void Init(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public static void ToggleEditMode(bool enabled)
        {
            IsEditMode = enabled;

            foreach (var mod in ModuleManager.Modules)
            {
                if (mod.ResizeHandle != null)
                    mod.ResizeHandle.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            }

            if (!enabled)
                ModuleManager.SaveAllModulePositions();
        }

        public static void ToggleManagerMode(bool enabled)
        {
            IsManagerMode = enabled;

            if (_mainWindow == null) return;

            if (enabled)
            {
                var view = new ModuleManagerView();
                view.PopulateModuleList();
                _mainWindow.ShowOverlayPanel(view);
            }
            else
                _mainWindow.HideOverlayPanel();
        }

        public static void ToggleSettingsMode(bool enabled)
        {
            IsSettingsMode = enabled;

            if (_mainWindow == null) return;

            if (enabled)
                _mainWindow.ShowOverlayPanel(new SettingsView());
            else
                _mainWindow.HideOverlayPanel();
        }

        public static void ToggleExclusiveMode(string mode, ToggleButton editBtn, ToggleButton managerBtn, ToggleButton settingsBtn, Border taskbar, OverlayWindow overlay)
        {
            if (mode == "Visible")
            {
                bool isNowVisible = taskbar.Visibility != Visibility.Visible;
                taskbar.Visibility = isNowVisible ? Visibility.Visible : Visibility.Collapsed;
                overlay.SetClickThrough(!isNowVisible);
            }

            if (mode != "Edit" && editBtn.IsChecked == true)
            {
                editBtn.IsChecked = false;
                ToggleEditMode(false);
            }

            if (mode != "Manager" && managerBtn.IsChecked == true)
            {
                managerBtn.IsChecked = false;
                ToggleManagerMode(false);
            }

            if (mode != "Settings" && settingsBtn.IsChecked == true)
            {
                settingsBtn.IsChecked = false;
                ToggleSettingsMode(false);
            }
        }
    }
}
