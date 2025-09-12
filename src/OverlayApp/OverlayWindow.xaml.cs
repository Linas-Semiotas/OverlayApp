using OverlayApp.Helpers;

namespace OverlayApp
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
            SetClickThrough(true);
        }

        public Canvas ModuleHost => ModuleHostCanvas;

        public void SetClickThrough(bool enabled)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            IntPtr style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);

            // Ensure WS_EX_LAYERED is always set (required for transparency to work)
            style |= NativeMethods.WS_EX_LAYERED;

            if (enabled)
                style |= NativeMethods.WS_EX_TRANSPARENT;
            else
                style &= ~NativeMethods.WS_EX_TRANSPARENT;

            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, style);
        }
    }
}
