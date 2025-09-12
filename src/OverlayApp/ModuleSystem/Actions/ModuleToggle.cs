using OverlayApp.Managers;

namespace OverlayApp.ModuleSystem.Actions
{
    public static class ModuleToggle
    {
        public static void Toggle(ModuleState mod, bool enabled)
        {
            mod.IsEnabled = enabled;

            var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
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

            if (!enabled)
                AppStateManager.RemoveFromOpenModules(mod.Id);
        }
    }
}