using System;
using System.IO;

namespace LabSystem.Core
{
    public static class FileUtilities
    {
        public static string FindFileUpwards(params string[] pathParts)
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, Path.Combine(pathParts));
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        public static string GetWritableDataDirectory()
        {
            object dataDirObj = AppDomain.CurrentDomain.GetData("DataDirectory");
            string dataDirectory = dataDirObj != null ? dataDirObj.ToString() : null;
            if (string.IsNullOrEmpty(dataDirectory))
            {
                dataDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            return dataDirectory;
        }
    }
}
