namespace OverlayApp.ModuleSystem
{
    public class ModuleState
    {
        public string Id { get; set; } = "";
        public string? DisplayName { get; set; }
        public string? Version { get; set; }

        public bool IsPersistent { get; set; }
        public bool IsEnabled { get; set; }

        public Position? Position { get; set; }
        public SizeConfig? Size { get; set; }

        public string FolderPath { get; set; } = string.Empty;
        public string IconPath => System.IO.Path.Combine(FolderPath, "icon.png");
        public string ConfigPath => System.IO.Path.Combine(FolderPath, "config.json");

        [JsonIgnore]
        public string StatePath => System.IO.Path.Combine("Data/Modules", Id, "state.json");

        public string XamlClassName => $"{char.ToUpper(Id[0])}{Id.Substring(1)}Module";

        // Runtime-only (set by ModuleManager)
        [JsonIgnore] public bool IsShown { get; set; } = false;
        [JsonIgnore] public System.Windows.Controls.UserControl? Control { get; set; }
        [JsonIgnore] public Canvas? Container { get; set; }
        [JsonIgnore] public ToggleButton? ToggleButton { get; set; }
        [JsonIgnore] public Border? ResizeHandle { get; set; }
    }

    public class Position
    {
        public double Left { get; set; }
        public double Top { get; set; }
    }

    public class SizeConfig
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double MinWidth { get; set; }
        public double MinHeight { get; set; }
    }

    public static class ModuleScanner
    {
        public static List<ModuleState> Scan(string modulesPath)
        {
            var modules = new List<ModuleState>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var dir in Directory.GetDirectories(modulesPath))
            {
                var stateFile = System.IO.Path.Combine("Data/Modules", System.IO.Path.GetFileName(dir), "state.json");
                if (!File.Exists(stateFile)) continue;

                try
                {
                    var json = File.ReadAllText(stateFile);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var module = new ModuleState
                    {
                        Id = System.IO.Path.GetFileName(dir),
                        Version = root.GetProperty("version").GetString(),
                        DisplayName = root.GetProperty("displayName").GetString(),
                        IsEnabled = root.GetProperty("isEnabled").GetBoolean(),
                        IsPersistent = root.GetProperty("isPersistent").GetBoolean(),
                        Position = JsonSerializer.Deserialize<Position>(root.GetProperty("position").GetRawText(), options),
                        Size = JsonSerializer.Deserialize<SizeConfig>(root.GetProperty("size").GetRawText(), options),
                        FolderPath = dir
                    };

                    modules.Add(module);
                }
                catch
                {
                    continue;
                }
            }

            return modules;
        }
    }
}
