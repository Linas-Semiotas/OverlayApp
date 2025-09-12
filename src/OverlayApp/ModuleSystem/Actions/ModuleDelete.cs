using OverlayApp.Controls;
using OverlayApp.Helpers;
using OverlayApp.Managers;
using static OverlayApp.Controls.MyMessageBox;

namespace OverlayApp.ModuleSystem.Actions
{
    public static class ModuleDelete
    {
        public static void Delete(ModuleState mod, Action refreshAfterDelete)
        {
            string moduleDataPath = $"Data/Modules/{mod.Id}";
            bool deleteData = false;

            if (Directory.Exists(moduleDataPath))
            {
                AskToDeleteUserData(mod, () =>
                {
                    deleteData = true;
                    AskToConfirmDelete(mod, moduleDataPath, deleteData, refreshAfterDelete);
                },
                () =>
                {
                    deleteData = false;
                    AskToConfirmDelete(mod, moduleDataPath, deleteData, refreshAfterDelete);
                });
            }
            else
            {
                AskToConfirmDelete(mod, moduleDataPath, deleteData, refreshAfterDelete);
            }
        }

        private static void AskToDeleteUserData(ModuleState mod, Action onDelete, Action onKeep)
        {
            var dataBox = new MyMessageBox(
                $"Delete user data for module '{mod.DisplayName ?? mod.Id}'?\nThis includes saved state, settings, and files.",
                MessageBoxType.Info,
                MessageBoxStyle.Standard,
                "Delete Data", "Keep");

            dataBox.OnResult = (choice) =>
            {
                if (choice == "Delete Data")
                    onDelete();
                else
                    onKeep();
            };

            MessageBoxHelper.Show(dataBox);
        }

        private static void AskToConfirmDelete(ModuleState mod, string moduleDataPath, bool deleteData, Action refreshAfterDelete)
        {
            var message = "To re-import it, a restart is required.\n\n" +
                          $"Delete module '{mod.DisplayName ?? mod.Id}'?\n\n";

            var confirmBox = new MyMessageBox(message, MessageBoxType.Info, MessageBoxStyle.Standard,
                                              "Delete and Restart", "Delete Only", "Cancel");

            confirmBox.OnResult = (choice) =>
            {
                if (choice == "Cancel") return;

                try
                {
                    TryDeleteFiles(mod.FolderPath, moduleDataPath, deleteData);
                    if (choice == "Delete and Restart")
                        AppHelper.RestartApp();
                    else
                        refreshAfterDelete();
                }
                catch
                {
                    MarkFilesForDeletion(mod.FolderPath, moduleDataPath, deleteData);
                    if (choice == "Delete and Restart")
                        AppHelper.RestartApp();
                    else
                        MessageBoxHelper.ShowInfo("Module marked for deletion. Restart to complete.", refreshAfterDelete);
                }

                AppStateManager.RemoveFromOpenModules(mod.Id);
            };

            MessageBoxHelper.Show(confirmBox);
        }

        private static void TryDeleteFiles(string folderPath, string moduleDataPath, bool deleteData)
        {
            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);

            if (deleteData && Directory.Exists(moduleDataPath))
                Directory.Delete(moduleDataPath, true);
        }

        private static void MarkFilesForDeletion(string folderPath, string moduleDataPath, bool deleteData)
        {
            if (Directory.Exists(folderPath))
                File.WriteAllText(System.IO.Path.Combine(folderPath, ".deleteonrestart"), "");

            if (deleteData && Directory.Exists(moduleDataPath))
                File.WriteAllText(System.IO.Path.Combine(moduleDataPath, ".deleteonrestart"), "");
        }
    }

}