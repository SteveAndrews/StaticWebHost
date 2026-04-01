namespace StaticWebHost
{
    public static class Utils
    {
        public static void EnsureDirectory(string fileOrDirectoryPath)
        {
            var dir = Path.HasExtension(fileOrDirectoryPath)
                ? Path.GetDirectoryName(fileOrDirectoryPath)
                : fileOrDirectoryPath;

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
