using OverlayApp.Managers;
using OverlayApp.SDK;

namespace OverlayApp
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            System.IO.Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppContext.BaseDirectory, "Modules"));
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "Modules"));

            AppStateManager.Load();
            ThemeManager.ApplyThemeFromState();
            Ui.Current = new UiServiceImpl();
        }

    }

}
