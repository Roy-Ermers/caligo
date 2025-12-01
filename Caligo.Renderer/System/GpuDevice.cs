using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL3;

namespace Caligo.Renderer.System;

public class GpuDevice: IDisposable
{
    private nint _deviceHandle;
    private nint? _windowHandle;
    private Stopwatch _stopwatch;

    public double Elapsed => _stopwatch.Elapsed.TotalSeconds;
    
    CancellationTokenSource _cancellationToken;

    internal GpuDevice(nint windowHandle) : this()
    {
        _windowHandle = windowHandle;
        SDL.ClaimWindowForGPUDevice(_deviceHandle, windowHandle);
    }

    internal GpuDevice(Window window) : this(window.Handle) {}
    
    internal GpuDevice()
    {
        _deviceHandle = SDL.CreateGPUDevice(SDL.GPUShaderFormat.SPIRV, true, null);
        _cancellationToken = new CancellationTokenSource();
        _stopwatch = new Stopwatch();
    }
    
    internal void StartRenderLoop()
    {
        var taskFactory = new TaskFactory();
        
        taskFactory.StartNew(RunLoop, TaskCreationOptions.LongRunning);
    }

    private void RunLoop()
    {
        _stopwatch.Restart();
        if(!_windowHandle.HasValue)
            throw new InvalidOperationException("GPU Device is not associated with a window.");
        
        while (!_cancellationToken.IsCancellationRequested)
        {
            var commandBuffer = SDL.AcquireGPUCommandBuffer(_deviceHandle);
            SDL.AcquireGPUSwapchainTexture(commandBuffer, _windowHandle.Value, out var texture, out uint width, out uint height);

            if (texture != nint.Zero)
            {
                var colorTargetInfo = new SDL.GPUColorTargetInfo()
                {
                    Texture = texture,
                    LoadOp = SDL.GPULoadOp.Clear,
                    StoreOp = SDL.GPUStoreOp.Store,
                    ClearColor = new SDL.FColor()
                    {
                        R = MathF.Sin((float)Elapsed) * 0.5f + 0.5f,
                        G = MathF.Sin((float)Elapsed + MathF.PI / 2f) * 0.5f + 0.5f,
                        B = MathF.Sin((float)Elapsed + MathF.PI) * 0.5f + 0.5f,
                        A = 1f
                    }
                };

                var ptr = SDL.StructureToPointer<SDL.GPUColorTargetInfo>(colorTargetInfo);
                var renderPass = SDL.BeginGPURenderPass(commandBuffer, ptr, 1, 0);
                Marshal.FreeHGlobal(ptr);
                
                SDL.EndGPURenderPass(renderPass);
            }

            SDL.SubmitGPUCommandBuffer(commandBuffer);
        }
    }
    
    public void Dispose()
    {
        _cancellationToken.Cancel();
        SDL.DestroyGPUDevice(_deviceHandle);
    }
}