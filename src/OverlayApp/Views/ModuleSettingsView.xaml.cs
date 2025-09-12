using OverlayApp.ModuleSystem;

namespace OverlayApp.Views
{
    public partial class ModuleSettingsView : UserControl
    {
        public ModuleSettingsView()
        {
            InitializeComponent();
            LoadModulesWithSettings();

            // Optional: wire buttons (you can fill these in later)
            ModuleSettingsReset.Click += ModuleSettingsReset_Click;
            ModuleSettingsSave.Click += ModuleSettingsSave_Click;
        }

        // =========================================
        // Populate the combo with modules that have a Settings UserControl
        // =========================================
        private void LoadModulesWithSettings()
        {
            ModuleSelectorComboBox.Items.Clear();

            var items = ModuleManager.Modules
                .Where(mod =>
                {
                    var dllPath = System.IO.Path.Combine(mod.FolderPath, $"{mod.Id}Module.dll");
                    if (!File.Exists(dllPath)) return false;

                    try
                    {
                        var asm = Assembly.LoadFrom(dllPath);
                        return asm.GetTypes().Any(t =>
                            t.Name == "Settings" &&
                            typeof(UserControl).IsAssignableFrom(t));
                    }
                    catch
                    {
                        return false;
                    }
                })
                .ToList();

            if (!items.Any())
            {
                ModuleSelectorComboBox.Items.Add(new ComboBoxItem
                {
                    Content = "No settings",
                    IsEnabled = false
                });
                ModuleSelectorComboBox.SelectedIndex = 0;
                return;
            }

            foreach (var mod in items)
            {
                ModuleSelectorComboBox.Items.Add(new ComboBoxItem
                {
                    Content = mod.DisplayName ?? mod.Id,
                    Tag = mod
                });
            }

            // Select first item and trigger load
            ModuleSelectorComboBox.SelectedIndex = 0;
            // Fire handler once for initial selection
            ModuleSelectorComboBox_SelectionChanged(
                ModuleSelectorComboBox,
                new SelectionChangedEventArgs(Selector.SelectionChangedEvent, Array.Empty<object>(), Array.Empty<object>()));
        }

        // =========================================
        // Selection changed => create Settings control and push JSON into it
        // =========================================
        private void ModuleSelectorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModuleSelectorComboBox.SelectedItem is not ComboBoxItem { Tag: ModuleState mod })
                return;

            try
            {
                var control = CreateSettingsControl(mod);
                if (control == null)
                {
                    ModuleSettingsHost.Content = new TextBlock
                    {
                        Text = "No valid Settings UI found.",
                        Foreground = Brushes.Gray,
                        FontSize = 14,
                        Margin = new Thickness(10)
                    };
                    return;
                }

                ModuleSettingsHost.Content = control;

                var json = LoadSettingsJson(mod);
                TryPushSettingsIntoControl(control, json);
            }
            catch (Exception ex)
            {
                ModuleSettingsHost.Content = new TextBlock
                {
                    Text = $"Error loading settings: {ex.Message}",
                    Foreground = Brushes.Red,
                    FontSize = 14,
                    Margin = new Thickness(10)
                };
            }
        }

        // =========================================
        // Helper: locate settings.json (Data first, then packaged)
        // =========================================
        private static string LoadSettingsJson(ModuleState mod)
        {
            var dataJson = System.IO.Path.Combine("Data/Modules", mod.Id, "settings.json");
            if (File.Exists(dataJson))
                return File.ReadAllText(dataJson);

            var packagedJson = System.IO.Path.Combine(mod.FolderPath, "settings.json");
            if (File.Exists(packagedJson))
                return File.ReadAllText(packagedJson);

            return "{}";
        }

        // =========================================
        // Helper: create the module’s Settings UserControl
        // =========================================
        private static UserControl? CreateSettingsControl(ModuleState mod)
        {
            var dllPath = System.IO.Path.Combine(mod.FolderPath, $"{mod.Id}Module.dll");
            var asm = Assembly.LoadFrom(dllPath);
            var type = asm.GetTypes().FirstOrDefault(t =>
                t.Name == "Settings" &&
                typeof(UserControl).IsAssignableFrom(t));
            return type != null ? (UserControl)Activator.CreateInstance(type)! : null;
        }

        // =========================================
        // Helper: call Settings.Set(...) if present
        //  - Prefer Set(JsonObject), fallback Set(string)
        //  - Optionally call RefreshUI() if module exposes it
        // =========================================
        private static void TryPushSettingsIntoControl(UserControl control, string json)
        {
            var type = control.GetType();

            // Prefer Set(JsonObject)
            var miJsonObj = type.GetMethod("Set", new[] { typeof(JsonObject) });
            if (miJsonObj != null)
            {
                JsonObject payload;
                try { payload = JsonNode.Parse(json)?.AsObject() ?? new JsonObject(); }
                catch { payload = new JsonObject(); }

                miJsonObj.Invoke(control, new object[] { payload });
                type.GetMethod("RefreshUI", Type.EmptyTypes)?.Invoke(control, null);
                return;
            }

            // Fallback: Set(string)
            var miString = type.GetMethod("Set", new[] { typeof(string) });
            if (miString != null)
            {
                miString.Invoke(control, new object[] { json });
                type.GetMethod("RefreshUI", Type.EmptyTypes)?.Invoke(control, null);
            }
            // If neither exists, we silently do nothing.
        }

        // =========================================
        // Bottom buttons (you can wire these later to your SettingsService)
        // =========================================
        private void ModuleSettingsReset_Click(object sender, RoutedEventArgs e)
        {
            if (ModuleSelectorComboBox.SelectedItem is not ComboBoxItem { Tag: ModuleState mod }) return;

            // Reload packaged defaults and push again
            var packaged = System.IO.Path.Combine(mod.FolderPath, "settings.json");
            var json = File.Exists(packaged) ? File.ReadAllText(packaged) : "{}";

            if (ModuleSettingsHost.Content is UserControl uc)
                TryPushSettingsIntoControl(uc, json);

            // If you also want to immediately persist to Data/Modules:
            File.WriteAllText(System.IO.Path.Combine("Data/Modules", mod.Id, "settings.json"), json);
        }

        private void ModuleSettingsSave_Click(object sender, RoutedEventArgs e)
        {
            if (ModuleSelectorComboBox.SelectedItem is not ComboBoxItem { Tag: ModuleState mod }) return;
            if (ModuleSettingsHost.Content is not UserControl uc) return;

            var type = uc.GetType();
            JsonObject? payload = null;

            // Prefer BuildPayload(): JsonObject
            var buildPayloadJsonObj = type.GetMethod("BuildPayload", Type.EmptyTypes);
            if (buildPayloadJsonObj != null)
            {
                var result = buildPayloadJsonObj.Invoke(uc, null);
                if (result is JsonObject jo)
                    payload = jo;
            }
            else
            {
                // Fallback: BuildPayloadString(): string
                var buildPayloadString = type.GetMethod("BuildPayloadString", Type.EmptyTypes);
                if (buildPayloadString != null)
                {
                    var result = buildPayloadString.Invoke(uc, null) as string ?? "{}";
                    payload = JsonNode.Parse(result)?.AsObject();
                }
            }

            if (payload == null) return;

            // Write settings.json to Data/Modules
            var outPath = System.IO.Path.Combine("Data/Modules", mod.Id, "settings.json");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath)!);
            File.WriteAllText(outPath, payload.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            // --- STEP 1B: push into live module if running ---
            var live = ModuleManager.Modules.FirstOrDefault(x => x.Id == mod.Id)?.Control;
            if (live != null)
            {
                var apply = live.GetType().GetMethod("ApplySettings", new[] { typeof(JsonObject) });
                if (apply != null)
                    apply.Invoke(live, new object[] { payload });
            }
        }

    }
}
