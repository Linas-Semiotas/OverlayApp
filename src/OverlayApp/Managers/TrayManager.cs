using OverlayApp.Helpers;
using OverlayApp.Managers;
using OverlayApp.ModuleSystem;

namespace OverlayApp.Tray
{
    public class TrayManager : IDisposable
    {
        private readonly MainWindow _mainWindow;
        private readonly OverlayWindow _overlayWindow;
        private readonly StackPanel _taskbarPanel;
        private readonly ComboBox _monitorSelector;
        private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
        private int _currentScreenIndex;

        public TrayManager(MainWindow mainWindow, OverlayWindow overlayWindow, StackPanel taskbarPanel, int currentScreenIndex, ComboBox monitorSelector)
        {
            _mainWindow = mainWindow;
            _overlayWindow = overlayWindow;
            _taskbarPanel = taskbarPanel;
            _currentScreenIndex = currentScreenIndex;
            _monitorSelector = monitorSelector;

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon("Assets/Favicon/icon.ico"),
                Visible = true,
                Text = "OverlayApp"
            };

            BuildContextMenu();
        }

        private void BuildContextMenu()
        {
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var screenMenu = new System.Windows.Forms.ToolStripMenuItem("Screen");
            var screens = ScreenHelper.GetAllScreens();

            for (int i = 0; i < screens.Length; i++)
            {
                int screenIndex = i;
                string label = $"Screen {i + 1}";

                var item = new System.Windows.Forms.ToolStripMenuItem(label)
                {
                    Checked = i == _currentScreenIndex
                };
                item.Click += (_, __) => ChangeScreen(screenIndex);

                screenMenu.DropDownItems.Add(item);
            }

            contextMenu.Items.Add(screenMenu);

            var refreshItem = new System.Windows.Forms.ToolStripMenuItem("Refresh");
            refreshItem.Click += (_, __) => RefreshApp();
            contextMenu.Items.Add(refreshItem);

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (_, __) => ExitApp();
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        public void ChangeScreen(int index)
        {
            _currentScreenIndex = index;
            ScreenHelper.SetWindowToScreen(_mainWindow, index);
            ScreenHelper.SetWindowToScreen(_overlayWindow, index);

            UpdateScreenCheckmarks();
            _monitorSelector.SelectedIndex = index;
        }

        private void UpdateScreenCheckmarks()
        {
            if (_notifyIcon.ContextMenuStrip?.Items[0] is System.Windows.Forms.ToolStripMenuItem screenMenu)
            {
                for (int i = 0; i < screenMenu.DropDownItems.Count; i++)
                {
                    if (screenMenu.DropDownItems[i] is System.Windows.Forms.ToolStripMenuItem item)
                        item.Checked = (i == _currentScreenIndex);
                }
            }
        }

        private void RefreshApp()
        {
            ModeManager.ToggleEditMode(false);
            _mainWindow.VisibilityToggleButton.IsChecked = false;
            _mainWindow.Taskbar.Visibility = Visibility.Collapsed;
            _taskbarPanel.Children.Clear();
            _overlayWindow.SetClickThrough(true);

            ScreenHelper.SetWindowToScreen(_overlayWindow, _currentScreenIndex);
            ModuleManager.RefreshModules(_overlayWindow.ModuleHost, _taskbarPanel);
        }

        private void ExitApp()
        {
            Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
