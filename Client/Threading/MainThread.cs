using System.Collections.Concurrent;

namespace Caligo.Client.Threading;

public class MainThread
{
    private readonly ConcurrentQueue<Action> _actions = [];
    private readonly EventWaitHandle _mainThreadLock = new(false, EventResetMode.AutoReset);
    private static MainThread? _instance;

    public MainThread()
    {
        _instance = this;
    }

    public static void Invoke(Action action)
    {
        if (_instance == null)
            throw new Exception("Main thread not initialized");

        BeginInvoke(action);
        _instance._mainThreadLock.WaitOne();
    }

    public static void BeginInvoke(Action action)
    {
        if (_instance == null) throw new NullReferenceException("Main thread not initialized");
        _instance._actions.Enqueue(action);
    }

    public void Update()
    {
        while (_actions.TryDequeue(out var action))
            action();

        _mainThreadLock.Set();
    }
}