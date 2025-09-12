namespace OverlayApp.Helpers
{
    public static class ScreenHelper
    {
        public static System.Windows.Forms.Screen[] GetAllScreens()
        {
            return System.Windows.Forms.Screen.AllScreens;
        }

        public static void SetWindowToScreen(Window window, int screenIndex)
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            if (screenIndex < 0 || screenIndex >= screens.Length)
                screenIndex = 0;

            var wa = screens[screenIndex].WorkingArea;
            window.Left = wa.Left;
            window.Top = wa.Top;
            window.Width = wa.Width;
            window.Height = wa.Height;
        }

        public static int GetScreenIndexFromWindow(Window window)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            var currentScreen = System.Windows.Forms.Screen.FromHandle(hwnd);
            var screens = System.Windows.Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
                if (screens[i].DeviceName == currentScreen.DeviceName)
                    return i;
            return 0;
        }

        public static void PopulateComboBox(ComboBox selector, int selectedIndex)
        {
            selector.Items.Clear();
            var screens = GetAllScreens();

            for (int i = 0; i < screens.Length; i++)
            {
                selector.Items.Add(new ComboBoxItem
                {
                    Content = $"Screen {i + 1}",
                    Tag = i
                });
            }

            selector.SelectedIndex = selectedIndex;
        }
    }
}