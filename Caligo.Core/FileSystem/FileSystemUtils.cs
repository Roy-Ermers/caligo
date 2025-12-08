using System.Diagnostics;

namespace Caligo.Core.FileSystem;

public static class FileSystemUtils
{
    public static void OpenFile(string path)
    {
        path = path.Replace('/', Path.DirectorySeparatorChar);
        // check if file exists
        if (!File.Exists(path)) throw new Exception("File does not exist: " + path);

        new Process
        {
            StartInfo = new ProcessStartInfo(path)
            {
                Verb = "open",
                UseShellExecute = true
            }
        }.Start();
    }

    public static void OpenDirectory(string path)
    {
        path = path.Replace('/', Path.DirectorySeparatorChar);
        // check if directory exists
        if (!Directory.Exists(path)) throw new Exception("Directory does not exist: " + path);

        new Process
        {
            StartInfo = new ProcessStartInfo(path)
            {
                Verb = "open",
                UseShellExecute = true
            }
        }.Start();
    }
}