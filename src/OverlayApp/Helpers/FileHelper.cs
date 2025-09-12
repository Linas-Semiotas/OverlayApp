namespace OverlayApp.Helpers
{
    public static class FileHelper
    {
        public static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);

            foreach (var file in Directory.GetFiles(source))
            {
                string destFile = System.IO.Path.Combine(dest, System.IO.Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                string destDir = System.IO.Path.Combine(dest, System.IO.Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }
    }
}
