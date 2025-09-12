using OverlayApp.Controls;
using OverlayApp.Helpers;
using static OverlayApp.Controls.MyMessageBox;
using IO = System.IO;

namespace OverlayApp.ModuleSystem.Actions
{
    public static class ModuleImport
    {
        public static void Import(string path, Action refreshAfterImport)
        {
            string modulesDir = "Modules";
            Directory.CreateDirectory(modulesDir);

            string id = ExtractModuleId(path);
            if (id.StartsWith("missing_"))
            {
                ShowErrorReason(id);
                return;
            }

            if (!ValidateDestination(id, modulesDir))
                return;

            try
            {
                var (configNode, moduleRoot) = ExtractAndParseConfig(path);
                configNode["isEnabled"] = false;

                var (result, hasSettingsUI) = ValidateModulePackage(moduleRoot, id);
                if (result.StartsWith("missing_"))
                {
                    ShowErrorReason(result);
                    return;
                }

                string dataPath = $"Data/Modules/{id}";
                string statePath = IO.Path.Combine(dataPath, "state.json");
                string settingsPath = IO.Path.Combine(dataPath, "settings.json");

                void writeSettings()
                {
                    if (hasSettingsUI)
                        WriteSettingsFile(settingsPath, moduleRoot);
                }
                void writeState() => WriteStateFile(statePath, dataPath, configNode);
                void finishSuccess() => MessageBoxHelper.ShowSuccess("Module imported successfully.", refreshAfterImport);

                OverwriteFolder(IO.Path.Combine(modulesDir, id), moduleRoot);

                if (File.Exists(statePath))
                {
                    if (!HandleExistingFiles(id, statePath, writeState, writeSettings, finishSuccess))
                        return;
                }
                else
                {
                    writeSettings();
                    writeState();
                    finishSuccess();
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"Import failed: {ex.Message}");
            }
        }

        public static string ExtractModuleId(string path)
        {
            if (!IO.Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                return "missing_config";

            string tempDir = IO.Path.Combine(IO.Path.GetTempPath(), "ModuleTemp_" + Guid.NewGuid());
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(path, tempDir);

                var configPath = Directory.GetFiles(tempDir, "config.json", SearchOption.AllDirectories).FirstOrDefault();
                if (string.IsNullOrEmpty(configPath))
                    return "missing_config";

                using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
                if (!doc.RootElement.TryGetProperty("id", out var idProp))
                    return "missing_config";

                string? id = idProp.GetString();
                if (string.IsNullOrWhiteSpace(id))
                    return "missing_config";

                return id;
            }
            catch
            {
                return "missing_config";
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        private static (string result, bool hasSettingsUI) ValidateModulePackage(string moduleRoot, string id)
        {
            string dllPath = IO.Path.Combine(moduleRoot, $"{id}Module.dll");
            if (!File.Exists(dllPath))
                return ("missing_dll", false);

            string iconPath = IO.Path.Combine(moduleRoot, "icon.png");
            if (!File.Exists(iconPath))
                return ("missing_png", false);

            bool hasSettingsUI = false;
            try
            {
                var asm = System.Reflection.Assembly.LoadFrom(dllPath);
                hasSettingsUI = asm.GetTypes().Any(t =>
                    t.Name == "Settings" &&
                    typeof(System.Windows.Controls.UserControl).IsAssignableFrom(t));
            }
            catch
            {
                hasSettingsUI = false;
            }

            if (hasSettingsUI)
            {
                string packagedSettings = IO.Path.Combine(moduleRoot, "settings.json");
                if (!File.Exists(packagedSettings))
                    return ("missing_settings", true);
            }

            return ("ok", hasSettingsUI);
        }

        private static void ShowErrorReason(string reasonCode)
        {
            string reason = reasonCode.Replace("missing_", "").ToUpperInvariant();
            MessageBoxHelper.ShowError($"Import failed: Missing {reason}.");
        }

        private static bool ValidateDestination(string id, string modulesDir)
        {
            string dest = IO.Path.Combine(modulesDir, id);
            string marker = IO.Path.Combine(dest, ".deleteonrestart");

            if (Directory.Exists(dest) && ModuleManager.Modules.Any(m => m.Id == id))
            {
                MessageBoxHelper.ShowError($"Module '{id}' already exists.\nPlease delete it first.");
                return false;
            }

            if (Directory.Exists(dest) && File.Exists(marker))
            {
                var confirmBox = new MyMessageBox(
                    $"Module '{id}' is marked for deletion.\nRestart now to import?",
                    MessageBoxType.Info, MessageBoxStyle.Standard, "Yes", "No");

                confirmBox.OnResult = (choice) =>
                {
                    if (choice == "Yes")
                        AppHelper.RestartApp();
                };

                MessageBoxHelper.Show(confirmBox);
                return false;
            }

            return true;
        }

        private static (JsonObject configNode, string moduleRoot) ExtractAndParseConfig(string path)
        {
            string tempDir = IO.Path.Combine(IO.Path.GetTempPath(), "ModuleTemp_" + Guid.NewGuid());
            System.IO.Compression.ZipFile.ExtractToDirectory(path, tempDir);

            string? configPath = Directory.GetFiles(tempDir, "config.json", SearchOption.AllDirectories).FirstOrDefault();
            if (configPath == null)
                throw new Exception("config.json missing in module.");

            string moduleRoot = IO.Path.GetDirectoryName(configPath)!;
            var configNode = JsonNode.Parse(File.ReadAllText(configPath))!.AsObject();

            return (configNode, moduleRoot);
        }

        private static void WriteStateFile(string statePath, string dataPath, JsonObject configNode)
        {
            Directory.CreateDirectory(dataPath);
            File.WriteAllText(statePath, configNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private static void WriteSettingsFile(string settingsPath, string moduleRoot)
        {
            Directory.CreateDirectory(IO.Path.GetDirectoryName(settingsPath)!);
            File.Copy(IO.Path.Combine(moduleRoot, "settings.json"), settingsPath, overwrite: true);
        }

        private static void OverwriteFolder(string dest, string moduleRoot)
        {
            if (Directory.Exists(dest))
                Directory.Delete(dest, true);

            FileHelper.CopyDirectory(moduleRoot, dest);
        }

        private static bool HandleExistingFiles(string id, string statePath, Action writeState, Action writeSettings, Action finishSuccess)
        {
            try
            {
                var parsed = JsonNode.Parse(File.ReadAllText(statePath))?.AsObject();
                bool valid = parsed != null &&
                             parsed.ContainsKey("isEnabled") &&
                             parsed["position"]?["left"] != null &&
                             parsed["position"]?["top"] != null &&
                             parsed["size"]?["width"] != null &&
                             parsed["size"]?["height"] != null;

                if (valid)
                {
                    var confirmBox = new MyMessageBox(
                        $"Previous state file for '{id}' found.\nDo you want to keep it?",
                        MessageBoxType.Info, MessageBoxStyle.Standard, "Keep", "Overwrite");

                    confirmBox.OnResult = (choice) =>
                    {
                        if (choice == "Overwrite")
                        {
                            writeState();
                            writeSettings();

                            // Detect Files/ folder for future handling
                            string filesDir = IO.Path.Combine("Data/Modules", id, "Files");
                            if (Directory.Exists(filesDir))
                            {
                                string[] existingFiles = Directory.GetFiles(filesDir)
                                  .Select(f => IO.Path.GetFileName(f))
                                  .ToArray();

                                if (existingFiles.Length > 0)
                                {
                                    string deleteListPath = IO.Path.Combine(filesDir, ".deleteList.txt");
                                    File.WriteAllLines(deleteListPath, existingFiles);
                                }
                            }
                        }
                        finishSuccess();
                    };

                    MessageBoxHelper.Show(confirmBox);
                }
                else
                {
                    var warningBox = new MyMessageBox(
                        $"Corrupted json file for '{id}' detected.\nOverwrite with default json file?",
                        MessageBoxType.Info, MessageBoxStyle.Standard, "Yes", "No");

                    warningBox.OnResult = (choice) =>
                    {
                        if (choice == "Yes")
                        {
                            writeState();
                            writeSettings() ;
                        }
                        finishSuccess();
                    };

                    MessageBoxHelper.Show(warningBox);
                }
            }
            catch
            {
                var errorBox = new MyMessageBox(
                    $"Unable to parse existing json file for '{id}'.\nOverwrite with default json file?",
                    MessageBoxType.Error, MessageBoxStyle.Standard, "Yes", "No");

                errorBox.OnResult = (choice) =>
                {
                    if (choice == "Yes")
                    {
                        writeState();
                        writeSettings();
                    }
                    finishSuccess();
                };

                MessageBoxHelper.Show(errorBox);
            }

            return true;
        }
    }
}