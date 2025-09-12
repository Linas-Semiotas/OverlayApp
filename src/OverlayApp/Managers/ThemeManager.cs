using OverlayApp.Loaders;

namespace OverlayApp.Managers
{
    public static class ThemeManager
    {
        private static readonly string ThemeFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Themes", "Json");
        private static string? _lastAppliedFileName;

        public static void ApplyThemeFromState()
        {
            var fileName = AppStateManager.Current["theme"]?.ToString();
            if (string.IsNullOrWhiteSpace(fileName)) return;

            string path = System.IO.Path.Combine(ThemeFolder, fileName);
            if (File.Exists(path))
            {
                JsonThemeLoader.LoadThemeFromJson(path);
                _lastAppliedFileName = fileName;
            }
        }

        public static List<ComboBoxItem> GetAvailableThemes()
        {
            var list = new List<ComboBoxItem>();
            if (!Directory.Exists(ThemeFolder))
                return list;

            var files = Directory.GetFiles(ThemeFolder, "*.json");

            Debug.WriteLine("Scanning themes from: " + ThemeFolder);
            Debug.WriteLine("Found files: " + string.Join(", ", Directory.GetFiles(ThemeFolder)));

            foreach (var file in files)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(file)
                    .Replace("_theme", "")
                    .Replace("_", " ");
                var item = new ComboBoxItem
                {
                    Content = Capitalize(name),
                    Tag = file,
                    IsSelected = IsCurrentTheme(file)
                };
                list.Add(item);
            }

            return list;
        }

        public static void ApplyThemeFromComboBoxItem(ComboBoxItem item)
        {
            if (item?.Tag is string path && File.Exists(path))
            {
                JsonThemeLoader.LoadThemeFromJson(path);
                string fileName = System.IO.Path.GetFileName(path);
                _lastAppliedFileName = fileName;
                AppStateManager.Current["theme"] = fileName;
                AppStateManager.Save();
            }
        }

        public static bool IsCurrentTheme(string path)
        {
            return string.Equals(System.IO.Path.GetFileName(path), _lastAppliedFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static string Capitalize(string input)
        {
            return char.ToUpper(input[0]) + input[1..];
        }
    }
}
