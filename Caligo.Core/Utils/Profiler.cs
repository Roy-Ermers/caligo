using System.Diagnostics;

namespace Caligo.Core.Utils;

public readonly record struct Profiler : IDisposable
{
    private readonly Stopwatch _stopwatch;

    public Profiler()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    public string Name { get; init; } = string.Empty;

    public readonly void Dispose()
    {
        _stopwatch.Stop();
        Debug.WriteLine($"[Profiler] {Name} took {_stopwatch.ElapsedMilliseconds} ms");
    }
}