namespace OverlayApp.Helpers
{
    public static class TrayHelper
    {
        // Add parameter: currentScreenIndex
        public static System.Windows.Forms.NotifyIcon CreateTrayIcon(
            Action<int> onScreenSelect,
            Action onRefresh,
            Action onExit,
            int currentScreenIndex,
            string tooltip = "OverlayApp",
            string iconPath = "Assets/Favicon/icon.ico")
        {
            var notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon(iconPath),
                Visible = true,
                Text = tooltip
            };

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var changeScreenMenu = new System.Windows.Forms.ToolStripMenuItem("Change Screen");
            var screens = System.Windows.Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                int screenIndex = i;
                string screenLabel = $"Screen {i + 1}" + (screens[i].Primary ? " (Primary)" : "");
                var item = new System.Windows.Forms.ToolStripMenuItem(screenLabel)
                {
                    Checked = (i == currentScreenIndex) // <-- Add this line
                };
                item.Click += (s, e) => onScreenSelect(screenIndex);
                changeScreenMenu.DropDownItems.Add(item);
            }
            contextMenu.Items.Add(changeScreenMenu);

            var refresh = new System.Windows.Forms.ToolStripMenuItem("Refresh");
            refresh.Click += (s, e) => onRefresh();
            contextMenu.Items.Add(refresh);

            var exit = new System.Windows.Forms.ToolStripMenuItem("Exit");
            exit.Click += (s, e) => onExit();
            contextMenu.Items.Add(exit);

            notifyIcon.ContextMenuStrip = contextMenu;
            return notifyIcon;
        }
    }
}
