namespace OverlayApp.Managers
{
    public static class AppStateManager
    {
        private const string StatePath = "Data/State/state.json";
        private const string DefaultPath = "Data/State/default_state.json";

        public static JsonObject Current { get; private set; } = new();

        public static void Load()
        {
            ValidateOrRecover();
            string stateJson = File.ReadAllText(StatePath);
            Current = JsonNode.Parse(stateJson)?.AsObject() ?? new JsonObject();
        }

        public static void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(Current, options);
            File.WriteAllText(StatePath, json);
        }

        public static void Reset()
        {
            File.Copy(DefaultPath, StatePath, overwrite: true);
            Load(); // refresh Current from new state
        }

        public static void ValidateOrRecover()
        {
            try
            {
                if (!File.Exists(StatePath))
                {
                    File.Copy(DefaultPath, StatePath);
                    return;
                }

                string stateJson = File.ReadAllText(StatePath);
                if (string.IsNullOrWhiteSpace(stateJson))
                    throw new Exception("Empty state file.");

                var state = JsonNode.Parse(stateJson)?.AsObject();
                var fallback = JsonNode.Parse(File.ReadAllText(DefaultPath))?.AsObject();

                if (state == null || fallback == null)
                    throw new Exception("Invalid or null structure.");

                bool modified = false;

                foreach (var kvp in fallback)
                {
                    if (!state.ContainsKey(kvp.Key))
                    {
                        state[kvp.Key] = JsonNode.Parse(kvp.Value?.ToJsonString() ?? "");
                        modified = true;
                    }
                }

                var keysToRemove = state.Where(kvp => !fallback.ContainsKey(kvp.Key)).Select(kvp => kvp.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    state.Remove(key);
                    modified = true;
                }

                if (modified)
                    File.WriteAllText(StatePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                Reset(); // fallback if validation fails
            }
        }

        public static void RemoveFromOpenModules(string moduleId)
        {
            if (!Current.TryGetPropertyValue("openModules", out var value))
                return;

            var openModules = Current["openModules"]?.AsArray();
            if (openModules == null) return;

            var updated = new JsonArray(openModules
                .Where(m => m?.ToString() != moduleId)
                .Select(m => JsonValue.Create(m?.ToString()))
                .ToArray());

            Current["openModules"] = updated;
            Save();
        }

    }
}