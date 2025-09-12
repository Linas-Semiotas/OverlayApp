using OverlayApp.SDK;
using OverlayApp.Controls;
using OverlayApp.Helpers;
using static OverlayApp.Controls.MyMessageBox;

namespace OverlayApp
{
    public sealed class UiServiceImpl : IUiService
    {
        public void Info(string m, string? t = null) => MessageBoxHelper.ShowInfo(m);
        public void Success(string m, string? t = null) => MessageBoxHelper.ShowSuccess(m);
        public void Error(string m, string? t = null) => MessageBoxHelper.ShowError(m);

        public bool Confirm(string m, string? t = null, string ok = "Yes", string cancel = "No")
        {
            var box = new MyMessageBox(m, MessageBoxType.Info, MessageBoxStyle.Standard, ok, cancel);
            bool result = false;
            var frame = new DispatcherFrame();
            box.OnResult = choice => { result = string.Equals(choice, ok, StringComparison.Ordinal); frame.Continue = false; };
            MessageBoxHelper.Show(box);
            Dispatcher.PushFrame(frame);
            return result;
        }

        public string Choose(string m, string? t = null, params string[] buttons)
        {
            if (buttons == null || buttons.Length == 0) buttons = new[] { "OK" };
            var box = new MyMessageBox(m, MessageBoxType.Info, MessageBoxStyle.Standard, buttons);
            string clicked = buttons[0];
            var frame = new DispatcherFrame();
            box.OnResult = choice => { clicked = choice!; frame.Continue = false; };
            MessageBoxHelper.Show(box);
            Dispatcher.PushFrame(frame);
            return clicked;
        }

        public string? Prompt(string m, string? t = null,
                              string ok = "OK", string cancel = "Cancel",
                              string? defaultText = null)
        {
            string? result = null;
            var frame = new DispatcherFrame();

            // Use the helper so host/close logic stays centralized
            MessageBoxHelper.ShowPrompt(
                m,
                onResult: val => { result = val; frame.Continue = false; },
                okLabel: ok,
                cancelLabel: cancel,
                defaultText: defaultText,
                type: MessageBoxType.Info,
                style: MessageBoxStyle.Standard);

            Dispatcher.PushFrame(frame);
            return result; // null => Cancel; non-null => OK text (may be empty)
        }
    }
}
