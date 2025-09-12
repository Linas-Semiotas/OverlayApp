namespace OverlayApp.Helpers
{
    public static class AppHelper
    {
        public static void RestartApp()
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exe))
            {
                Process.Start(exe);
                Application.Current.Shutdown();
            }
        }

        public static void DeleteMarkedFolders(string basePath)
        {
            if (!Directory.Exists(basePath)) return;

            foreach (var dir in Directory.GetDirectories(basePath))
            {
                string marker = System.IO.Path.Combine(dir, ".deleteonrestart");
                if (File.Exists(marker))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch
                    {
                        // Still locked, skip it
                    }
                }
            }
        }

        public static void CleanupDeleteLists(string basePath)
        {
            if (!Directory.Exists(basePath)) return;

            foreach (var moduleDir in Directory.GetDirectories(basePath))
            {
                string filesDir = System.IO.Path.Combine(moduleDir, "Files");
                string deleteListPath = System.IO.Path.Combine(filesDir, ".deleteList.txt");

                if (File.Exists(deleteListPath))
                {
                    try
                    {
                        var filesToDelete = File.ReadAllLines(deleteListPath);
                        foreach (var file in filesToDelete)
                        {
                            string filePath = System.IO.Path.Combine(filesDir, file);
                            try
                            {
                                if (File.Exists(filePath))
                                    File.Delete(filePath);
                            }
                            catch
                            {
                                // Locked or failed — skip for now
                            }
                        }

                        File.Delete(deleteListPath); // Delete the list once processed
                    }
                    catch
                    {
                        // Skip problematic list
                    }
                }
            }
        }

        public static void CleanupPendingModules()
        {
            DeleteMarkedFolders("Modules");
            DeleteMarkedFolders("Data/Modules");
            CleanupDeleteLists("Data/Modules");
        }

    }
}