namespace OverlayApp.Views
{
    public partial class SettingsView : UserControl
    {

        private readonly GeneralSettingsView _generalView = new();
        private readonly ModuleSettingsView _moduleView = new();

        public SettingsView()
        {
            InitializeComponent();

            GeneralSettingsButton.Checked += (_, _) =>
            {
                ModulesSettingsButton.IsChecked = false;
                SettingsContent.Content = _generalView;
            };

            ModulesSettingsButton.Checked += (_, _) =>
            {
                GeneralSettingsButton.IsChecked = false;
                SettingsContent.Content = _moduleView;
            };

            GeneralSettingsButton.Unchecked += (_, _) =>
            {
                if (ModulesSettingsButton.IsChecked == false)
                    GeneralSettingsButton.IsChecked = true;
            };

            ModulesSettingsButton.Unchecked += (_, _) =>
            {
                if (GeneralSettingsButton.IsChecked == false)
                    ModulesSettingsButton.IsChecked = true;
            };

            GeneralSettingsButton.IsChecked = true;
        }
    }
}
