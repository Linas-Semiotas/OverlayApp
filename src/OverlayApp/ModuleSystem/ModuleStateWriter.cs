using OverlayApp.ModuleSystem;

namespace OverlayApp.Helpers
{
    public static class ModuleStateWriter
    {
        public static void SaveAll(List<ModuleState> modules)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            foreach (var mod in modules)
            {
                if (mod.Position == null || mod.Container == null)
                    continue;

                mod.Position.Left = Canvas.GetLeft(mod.Container);
                mod.Position.Top = Canvas.GetTop(mod.Container);

                if (mod.Size != null && mod.Control != null)
                {
                    mod.Size.Width = mod.Control.Width;
                    mod.Size.Height = mod.Control.Height;
                }

                var cleanState = new
                {
                    id = mod.Id,
                    displayName = mod.DisplayName,
                    version = mod.Version,
                    isPersistent = mod.IsPersistent,
                    position = mod.Position,
                    size = mod.Size,
                    isEnabled = mod.IsEnabled
                };

                File.WriteAllText(mod.StatePath, JsonSerializer.Serialize(cleanState, options));
            }
        }
    }
}
