using OverlayApp.Helpers;
using OverlayApp.Managers;
using OverlayApp.ModuleSystem;
using OverlayApp.Tray;

namespace OverlayApp
{
    public partial class MainWindow : Window
    {
        private int _currentScreenIndex = 0;
        private OverlayWindow? _overlayWindow;
        private TrayManager? _trayManager;

        private HotkeyManager? _hotkeys;
        private bool _visButtonHidden = false;
        private bool _forceShowButtonWhileTaskbarVisible = false;

        public static MainWindow Instance => (MainWindow)Application.Current.MainWindow;
        public Panel MessageBoxHost => MessageBoxLayer;

        public MainWindow()
        {
            InitializeComponent();
            RegisterWindowEvents();
            AppHelper.CleanupPendingModules();
        }

        // ============================
        // Lifecycle / Initialization
        // ============================

        private void RegisterWindowEvents()
        {
            Loaded += (_, __) => Initialize();
            Closed += (_, __) =>
            {
                _hotkeys?.Dispose();
                _trayManager?.Dispose();
            };
            Application.Current.Exit += (_, __) =>
            {
                _hotkeys?.Dispose();
                _trayManager?.Dispose();
            };
        }

        private void Initialize()
        {
            _currentScreenIndex = ScreenHelper.GetScreenIndexFromWindow(this);
            ScreenHelper.SetWindowToScreen(this, _currentScreenIndex);

            ScreenHelper.PopulateComboBox(MonitorSelector, _currentScreenIndex);

            _overlayWindow = new OverlayWindow();
            _overlayWindow.Show();
            ScreenHelper.SetWindowToScreen(_overlayWindow, _currentScreenIndex);

            ModuleManager.InitModules(_overlayWindow.ModuleHost, TaskbarTaskButtonsPanel);
            _overlayWindow.SetClickThrough(true);

            _trayManager = new TrayManager(this, _overlayWindow, TaskbarTaskButtonsPanel, _currentScreenIndex, MonitorSelector);

            ModeManager.Init(this);

            _hotkeys = HotkeyManager.Attach(
                this,
                onToggleVis: () => Dispatcher.Invoke(ToggleVisibilityByHotkey),
                onHideBtn: () => Dispatcher.Invoke(ToggleVisibilityButtonPresence));

            _forceShowButtonWhileTaskbarVisible = Taskbar.Visibility == Visibility.Visible;
            UpdateVisibilityButtonForTaskbarState(Taskbar.Visibility == Visibility.Visible);
        }

        public void ReloadModules()
        {
            ModuleManager.RefreshModules(_overlayWindow!.ModuleHost, TaskbarTaskButtonsPanel);
        }

        // ============================
        // Event Handlers
        // ============================

        private void ToggleTaskbar_Click(object sender, RoutedEventArgs e)
        {
            bool wasVisible = Taskbar.Visibility == Visibility.Visible;

            ToggleExclusiveMode("Visible");

            bool nowVisible = Taskbar.Visibility == Visibility.Visible;
            _forceShowButtonWhileTaskbarVisible = nowVisible;
            UpdateVisibilityButtonForTaskbarState(nowVisible);
        }

        private void EditToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleExclusiveMode("Edit");
            ModeManager.ToggleEditMode(EditToggleButton.IsChecked == true);
        }

        private void ManagerToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleExclusiveMode("Manager");
            ModeManager.ToggleManagerMode(ManagerToggleButton.IsChecked == true);
        }

        private void SettingsToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleExclusiveMode("Settings");
            ModeManager.ToggleSettingsMode(SettingsToggleButton.IsChecked == true);
        }

        private void MonitorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonitorSelector.SelectedItem is ComboBoxItem { Tag: int screenIndex })
                _trayManager?.ChangeScreen(screenIndex);
        }

        // ============================
        // Toggle Logic
        // ============================

        private void ToggleExclusiveMode(string mode)
        {
            ModeManager.ToggleExclusiveMode(mode, EditToggleButton, ManagerToggleButton, SettingsToggleButton, Taskbar, _overlayWindow!);
        }

        // ============================
        // Overlay Panel
        // ============================

        public void ShowOverlayPanel(UserControl content)
        {
            OverlayContentHost.Content = content;
            OverlayPanelHost.Visibility = Visibility.Visible;
        }

        public void HideOverlayPanel()
        {
            OverlayContentHost.Content = null;
            OverlayPanelHost.Visibility = Visibility.Collapsed;
        }

        // ============================
        // Hotkey actions
        // ============================

        private void ToggleVisibilityByHotkey()
        {
            VisibilityToggleButton.IsChecked = !(VisibilityToggleButton.IsChecked ?? false);
            ToggleTaskbar_Click(VisibilityToggleButton, new RoutedEventArgs());
        }

        private void ToggleVisibilityButtonPresence()
        {
            if (Taskbar.Visibility == Visibility.Visible) return;

            _visButtonHidden = !_visButtonHidden;

            UpdateVisibilityButtonForTaskbarState(Taskbar.Visibility == Visibility.Visible);
        }

        private void UpdateVisibilityButtonForTaskbarState(bool taskbarVisible)
        {
            bool showButton =
                taskbarVisible && _forceShowButtonWhileTaskbarVisible
                || (!taskbarVisible && !_visButtonHidden);

            VisibilityToggleButton.Visibility = showButton ? Visibility.Visible : Visibility.Collapsed;

            this.IsHitTestVisible = taskbarVisible || !_visButtonHidden;
        }
    }
}