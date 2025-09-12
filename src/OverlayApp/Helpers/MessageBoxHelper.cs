using OverlayApp.Controls;

namespace OverlayApp.Helpers
{
    public static class MessageBoxHelper
    {
        public static void Show(MyMessageBox box)
        {
            var host = MainWindow.Instance.MessageBoxHost;
            host.IsHitTestVisible = true;
            host.Children.Add(box);
            Panel.SetZIndex(box, 999);

            box.OnResult += _ =>
            {
                if (host.Children.Contains(box))
                    host.Children.Remove(box);

                if (host.Children.Count == 0)
                    host.IsHitTestVisible = false;
            };
        }

        public static void ShowInfo(string message, Action? after = null)
        {
            var box = MyMessageBox.CreateStandard(message, MyMessageBox.MessageBoxType.Info, MessageBoxButton.OK);
            box.OnResult = _ => after?.Invoke();
            Show(box);
        }

        public static void ShowError(string message, Action? after = null)
        {
            var box = MyMessageBox.CreateStandard(message, MyMessageBox.MessageBoxType.Error, MessageBoxButton.OK);
            box.OnResult = _ => after?.Invoke();
            Show(box);
        }

        public static void ShowSuccess(string message, Action? after = null)
        {
            var box = MyMessageBox.CreateStandard(message, MyMessageBox.MessageBoxType.Success, MessageBoxButton.OK);
            box.OnResult = _ => after?.Invoke();
            Show(box);
        }

        public static void ShowPrompt(string message,
                                      Action<string?> onResult,
                                      string okLabel = "OK",
                                      string cancelLabel = "Cancel",
                                      string? defaultText = null,
                                      MyMessageBox.MessageBoxType type = MyMessageBox.MessageBoxType.Info,
                                      MyMessageBox.MessageBoxStyle style = MyMessageBox.MessageBoxStyle.Standard)
        {
            var box = MyMessageBox.CreatePrompt(message, type, okLabel, cancelLabel, defaultText, style);
            box.OnResult = onResult;
            Show(box);
        }
    }
}
