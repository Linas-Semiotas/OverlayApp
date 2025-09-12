namespace OverlayApp.SDK
{
    public interface IUiService
    {
        void Info(string message, string? title = null);
        void Success(string message, string? title = null);
        void Error(string message, string? title = null);
        bool Confirm(string message, string? title = null, string ok = "Yes", string cancel = "No");
        string Choose(string message, string? title = null, params string[] buttons);
        string? Prompt(string message, string? title = null,
                       string ok = "OK", string cancel = "Cancel",
                       string? defaultText = null);
    }
}
