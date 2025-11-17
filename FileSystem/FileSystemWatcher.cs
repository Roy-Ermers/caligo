using WorldGen.Threading;

namespace WorldGen.FileSystem;

public class FileSystemWatcher
{
    private readonly System.IO.FileSystemWatcher _watcher;

    public event FileSystemEventHandler Changed = delegate { };

    public FileSystemWatcher(string path, NotifyFilters notifyfilter, string filter = "*")
    {
        _watcher = new System.IO.FileSystemWatcher(path)
        {
            Filter = filter,
            NotifyFilter = notifyfilter
        };

        _watcher.Changed += (sender, args) => MainThread.Invoke(delegate
        {
            Changed(sender, args);
        });

        _watcher.EnableRaisingEvents = true;
    }
}
