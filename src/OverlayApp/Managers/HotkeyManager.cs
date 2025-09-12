namespace OverlayApp.Managers
{
    public sealed class HotkeyManager : IDisposable
    {
        const int WM_HOTKEY = 0x0312;
        const uint MOD_CONTROL = 0x0002;
        const uint MOD_SHIFT = 0x0004;
        const uint VK_F3 = 0x72;
        const uint VK_F4 = 0x73;

        const int ID_TOGGLE_VIS = 1001; // Ctrl+Shift+F3
        const int ID_HIDE_BTN = 1002; // Ctrl+Shift+F4

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly IntPtr _hwnd;
        private readonly HwndSource _source;
        private readonly Action _onToggleVis;
        private readonly Action _onHideBtn;

        private HotkeyManager(IntPtr hwnd, HwndSource src, Action onToggleVis, Action onHideBtn)
        {
            _hwnd = hwnd;
            _source = src;
            _onToggleVis = onToggleVis;
            _onHideBtn = onHideBtn;
            _source.AddHook(WndProc);

            RegisterHotKey(_hwnd, ID_TOGGLE_VIS, MOD_CONTROL | MOD_SHIFT, VK_F3);
            RegisterHotKey(_hwnd, ID_HIDE_BTN, MOD_CONTROL | MOD_SHIFT, VK_F4);
        }

        public static HotkeyManager Attach(Window window, Action onToggleVis, Action onHideBtn)
        {
            var helper = new WindowInteropHelper(window);
            var src = HwndSource.FromHwnd(helper.Handle)!;
            return new HotkeyManager(helper.Handle, src, onToggleVis, onHideBtn);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                switch (wParam.ToInt32())
                {
                    case ID_TOGGLE_VIS: _onToggleVis(); handled = true; break;
                    case ID_HIDE_BTN: _onHideBtn(); handled = true; break;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            try { UnregisterHotKey(_hwnd, ID_TOGGLE_VIS); } catch { }
            try { UnregisterHotKey(_hwnd, ID_HIDE_BTN); } catch { }
            _source.RemoveHook(WndProc);
        }
    }
}
