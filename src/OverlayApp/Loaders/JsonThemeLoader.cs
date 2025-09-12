namespace OverlayApp.Loaders
{
    public static class JsonThemeLoader
    {
        public static void LoadThemeFromJson(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Theme file not found.", path);

            var json = File.ReadAllText(path);
            var colorDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (colorDict == null)
                throw new Exception("Invalid theme file format.");

            foreach (var kvp in colorDict)
            {
                var color = (Color)ColorConverter.ConvertFromString(kvp.Value);
                var brush = new SolidColorBrush(color);
                brush.Freeze(); // For performance

                Application.Current.Resources[$"C_{kvp.Key}"] = color;
                Application.Current.Resources[$"B_{kvp.Key}"] = brush;
            }
        }
    }
}