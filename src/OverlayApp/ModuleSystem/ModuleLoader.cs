namespace OverlayApp.ModuleSystem
{
    public static class ModuleLoader
    {
        public static UserControl LoadModuleControl(ModuleState state)
        {
            string dllPath = Directory.GetFiles(state.FolderPath, "*.dll").FirstOrDefault()
              ?? throw new FileNotFoundException("No DLL found in module folder.");
            if (!File.Exists(dllPath))
                throw new FileNotFoundException($"Module DLL not found: {dllPath}");

            var assembly = Assembly.LoadFrom(dllPath);
            string fullTypeName = $"OverlayApp.Modules.{state.Id}.{state.XamlClassName}";
            var moduleType = assembly.GetType(fullTypeName);

            if (moduleType == null)
                throw new Exception($"Type '{fullTypeName}' not found in {dllPath}");

            var control = Activator.CreateInstance(moduleType) as UserControl;
            if (control == null)
                throw new Exception($"Type '{fullTypeName}' is not a UserControl");

            if (state.Size != null)
            {
                control.Width = state.Size.Width;
                control.Height = state.Size.Height;
                control.MinWidth = state.Size.MinWidth;
                control.MinHeight = state.Size.MinHeight;
            }

            // Apply settings if module exposes ApplySettings(JsonObject)
            try
            {
                string settingsPath = System.IO.Path.Combine("Data/Modules", state.Id, "settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var node = JsonNode.Parse(json) as System.Text.Json.Nodes.JsonObject;

                    var m = control.GetType().GetMethod("ApplySettings",
                             System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (m != null && node != null && m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(System.Text.Json.Nodes.JsonObject))
                    {
                        m.Invoke(control, new object[] { node });
                    }
                }
            }
            catch { /* swallow – settings are optional */ }


            return control;
        }

        public static Canvas CreateModuleContainer(ModuleState state)
        {
            var canvas = new Canvas
            {
                Visibility = Visibility.Collapsed
            };

            if (state.Position != null)
            {
                Canvas.SetLeft(canvas, state.Position.Left);
                Canvas.SetTop(canvas, state.Position.Top);
            }

            return canvas;
        }

        public static ToggleButton CreateToggleButton(ModuleState state, Action<bool> onToggle)
        {
            var bitmap = new BitmapImage();
            using (var stream = new FileStream(System.IO.Path.GetFullPath(state.IconPath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // Important to allow cross-thread use and avoid locking the file
            }

            var image = new System.Windows.Controls.Image
            {
                Source = bitmap,
                Width = 36,
                Height = 36
            };

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            var button = new ToggleButton
            {
                Style = (Style)System.Windows.Application.Current.FindResource("CircleButton"),
                Content = image
            };

            button.Checked += (s, e) => onToggle(true);
            button.Unchecked += (s, e) => onToggle(false);

            return button;
        }
    }
}
