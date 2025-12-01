using System.Reflection;
using Caligo.Renderer.System;
using SDL3;

namespace Caligo.Renderer;

public class Renderer
{
    private readonly Window window = null!;
    private readonly GpuDevice gpuDevice = null!;
    public bool Running { get; private set; } = true;
    
    public Renderer()
    {
        SDL.Log($"Starting Caligo Renderer");
        SDL.Log($"SDL Version: {SDL.GetVersion()}");
        
        SDL.SetAppMetadata(
            "Caligo Renderer",
            "0.0.0",
            "caligo"
        );
        
        if (!SDL.Init(SDL.InitFlags.Video))
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
            return;
        }
        
        window = new Window("Caligo");
        gpuDevice = new GpuDevice(window);
    }

    public void Run()
    {
        gpuDevice.StartRenderLoop();
        while (Running)
        {
            while (SDL.PollEvent(out var e))
            {
                if (e.Type == (uint)SDL.EventType.Quit)
                {
                    Running = false;
                }
            }
        }
        
        gpuDevice.Dispose();
        window.Dispose();
        
        SDL.Quit();
    }
}