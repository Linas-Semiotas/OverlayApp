using OverlayApp.Helpers;
using OverlayApp.Managers;

namespace OverlayApp.ModuleSystem
{
    public static class ModuleManager
    {
        public static List<ModuleState> Modules { get; private set; } = new();

        public static void InitModules(Canvas hostCanvas, System.Windows.Controls.Panel taskbarPanel, string modulesPath = "Modules")
        {
            var scannedModules = ModuleScanner.Scan(modulesPath);
            var openModules = AppStateManager.Current["openModules"]?.AsArray()
                ?.Select(x => x?.ToString()!)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet() ?? new HashSet<string>();

            foreach (var mod in scannedModules)
            {
                mod.IsShown = openModules.Contains(mod.Id);

                try
                {
                    Modules.Add(mod);
                    InitializeModule(mod, hostCanvas, taskbarPanel, openModules);
                }
                catch
                {
                    continue;
                }
            }
        }

        private static void InitializeModule(ModuleState mod, Canvas hostCanvas, System.Windows.Controls.Panel taskbarPanel, HashSet<string> openModules)
        {
            mod.ToggleButton = ModuleLoader.CreateToggleButton(mod, isVisible =>
            {
                mod.IsShown = isVisible;
                if (mod.Container != null)
                    mod.Container.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

                if (isVisible) openModules.Add(mod.Id);
                else openModules.Remove(mod.Id);

                AppStateManager.Current["openModules"] = new JsonArray(openModules.Select(x => JsonValue.Create(x)).ToArray());
                AppStateManager.Save();
            });

            mod.ToggleButton.IsChecked = mod.IsShown;

            if (!mod.IsEnabled) return;

            mod.Control = ModuleLoader.LoadModuleControl(mod);
            mod.Container = ModuleLoader.CreateModuleContainer(mod);

            var grid = new Grid();
            grid.Children.Add(mod.Control);

            // Add resize handle
            var resizeHandle = new Border
            {
                Width = 8,
                Height = 8,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(1),
                Cursor = Cursors.SizeNWSE,
                Visibility = Visibility.Collapsed
            };
            resizeHandle.SetResourceReference(Border.BackgroundProperty, "B_BackgroundPrimary");
            resizeHandle.SetResourceReference(Border.BorderBrushProperty, "B_Border");

            mod.ResizeHandle = resizeHandle;
            grid.Children.Add(resizeHandle);
            mod.Container.Children.Add(grid);

            UIInteractionHelper.AttachDrag(mod.Container, () => ModeManager.IsEditMode);
            UIInteractionHelper.AttachResize(resizeHandle, mod.Control!, () => ModeManager.IsEditMode, mod.Size!.MinWidth, mod.Size.MinHeight);

            mod.Container.Visibility = mod.IsShown ? Visibility.Visible : Visibility.Collapsed;

            hostCanvas.Children.Add(mod.Container);
            taskbarPanel.Children.Add(mod.ToggleButton);
        }

        public static void ClearModules(Canvas hostCanvas, System.Windows.Controls.Panel taskbarPanel)
        {
            foreach (var mod in Modules)
            {
                if (mod.Container != null)
                    hostCanvas.Children.Remove(mod.Container);
                if (mod.ToggleButton != null)
                    taskbarPanel.Children.Remove(mod.ToggleButton);
            }

            Modules.Clear();
        }

        public static void RefreshModules(Canvas hostCanvas, System.Windows.Controls.Panel taskbarPanel, string modulesPath = "Modules")
        {
            ClearModules(hostCanvas, taskbarPanel);
            InitModules(hostCanvas, taskbarPanel, modulesPath);
        }

        public static void SaveAllModulePositions()
        {
            ModuleStateWriter.SaveAll(Modules);
        }
    }
}