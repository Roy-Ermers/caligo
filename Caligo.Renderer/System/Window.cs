using SDL3;

namespace Caligo.Renderer.System;

public class Window : IDisposable
{
    private nint _handle;
    public nint Handle => _handle;
    private nint _renderer;
    internal nint RendererHandle => _renderer;
    
    public string Title
    {
        get => SDL.GetWindowTitle(_handle);
        set => SDL.SetWindowTitle(_handle, value);
    }

    public (int X, int Y) Position
    {
        get => !SDL.GetWindowPosition(_handle, out var x, out var y) ? throw new InvalidOperationException() : (x, y);
        set => SDL.SetWindowPosition(_handle, value.X, value.Y);
    }
    
    public (int Width, int Height) Size
    {
        get => !SDL.GetWindowSize(_handle, out var w, out var h) ? throw new InvalidOperationException() : (w, h);
        set => SDL.SetWindowSize(_handle, value.Width, value.Height);
    }

    public Window(string title)
    {
        var displayMode = SDL.GetCurrentDisplayMode(0);
        CreateWindow(title, displayMode?.W ?? 800, displayMode?.H ?? 600);
    }
    
    public Window(string title, int width, int height) => CreateWindow(title, width, height);
    
    internal Window(nint windowHandle)
    {
        _handle = windowHandle;
    }
    
    private void CreateWindow(string title, int width, int height)
    {
        _handle = SDL.CreateWindow(title, width, height, SDL.WindowFlags.Resizable | SDL.WindowFlags.Maximized);

        if (_handle != nint.Zero) return;
        
        throw new ApplicationException("Failed to create window.")
        {
            Data =
            {
                {"SDL", SDL.GetError() }
            }
        };
    }
   
    public void Dispose()
    {
        SDL.DestroyWindow(_handle);
        SDL.DestroyRenderer(_renderer);
    }
}