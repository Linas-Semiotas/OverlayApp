using Microsoft.Win32;
using OverlayApp.ModuleSystem;
using OverlayApp.ModuleSystem.Actions;

namespace OverlayApp.Views
{
    public partial class ModuleManagerView : UserControl
    {
        public ModuleManagerView()
        {
            InitializeComponent();
            ImportModuleButton.Click += ImportModuleButton_Click;
        }

        // ===============================
        // Import Logic
        // ===============================
        private void ImportModuleButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Module (.zip)",
                Filter = "Module files (*.zip)|*.zip",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
                ModuleImport.Import(dialog.FileName, RefreshUI);
        }

        // ===============================
        // Toggle Logic
        // ===============================
        private void ToggleModule(ModuleState mod, bool enabled)
        {
            ModuleToggle.Toggle(mod, enabled);

            if (Application.Current.MainWindow is MainWindow main)
            {
                main.ReloadModules();
                PopulateModuleList();
            }
        }

        // ===============================
        // UI Refresh & Rebuild
        // ===============================
        private void RefreshUI()
        {
            if (Application.Current.MainWindow is MainWindow main)
            {
                main.ReloadModules();
                PopulateModuleList();
            }
        }

        public void PopulateModuleList()
        {
            ModuleListPanel.Children.Clear();

            foreach (var mod in ModuleManager.Modules)
            {
                var grid = CreateModuleRow(mod);
                ModuleListPanel.Children.Add(grid);
            }
        }

        private Grid CreateModuleRow(ModuleState mod)
        {
            var grid = new Grid { Height = 40, Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            // Toggle Checkbox
            var enabledCheckbox = new CheckBox
            {
                IsChecked = mod.IsEnabled,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Style = (Style)FindResource("CustomCheckBox")
            };
            enabledCheckbox.Checked += (s, e) => ToggleModule(mod, true);
            enabledCheckbox.Unchecked += (s, e) => ToggleModule(mod, false);
            Grid.SetColumn(enabledCheckbox, 0);
            grid.Children.Add(enabledCheckbox);

            // Module Name
            var nameText = new TextBlock
            {
                Text = mod.DisplayName ?? mod.Id,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Margin = new Thickness(5, 0, 0, 0)
            };
            nameText.SetResourceReference(TextBlock.ForegroundProperty, "B_Text");
            Grid.SetColumn(nameText, 1);
            grid.Children.Add(nameText);

            // Module Version
            var versionText = new TextBlock
            {
                Text = mod.Version ?? "-",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 14
            };
            versionText.SetResourceReference(TextBlock.ForegroundProperty, "B_Text");
            Grid.SetColumn(versionText, 2);
            grid.Children.Add(versionText);

            // Delete Button (if allowed)
            if (!mod.IsPersistent)
            {
                var deleteButton = new Button
                {
                    Content = "Delete",
                    Width = 60,
                    Height = 25,
                    BorderThickness = new Thickness(1),
                    Style = (Style)FindResource("HoverButtonStyle")
                };
                deleteButton.Click += (s, e) => ModuleDelete.Delete(mod, RefreshUI);
                Grid.SetColumn(deleteButton, 3);
                grid.Children.Add(deleteButton);
            }

            return grid;
        }
    }
}