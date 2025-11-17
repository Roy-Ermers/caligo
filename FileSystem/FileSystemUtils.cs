using System.Diagnostics;

namespace WorldGen.FileSystem;

public static class FileSystemUtils
{
    public static void OpenFile(string path)
    {
        path = path.Replace('/', Path.DirectorySeparatorChar);
        // check if file exists
        if (!File.Exists(path))
        {
            throw new System.Exception("File does not exist: " + path);
        }

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
        if (!Directory.Exists(path))
        {
            throw new System.Exception("Directory does not exist: " + path);
        }

        new Process
        {
            StartInfo = new ProcessStartInfo(path)
            {
                Verb = "open",
                UseShellExecute = true
            }
        }.Start();
    }

    static readonly string[] suffixes = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
    public static string FormatByteSize(long byteCount)
    {
        if (byteCount == 0)
            return "0" + suffixes[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 2);
        return (Math.Sign(byteCount) * num).ToString() + suffixes[place];
    }
}