using OpenTK.Graphics.OpenGL;

namespace Caligo.Client.Graphics;

public class FrameBuffer : IDisposable
{
    public int Handle { get; private set; }
    public Texture2D ColorTexture { get; private set; }
    public int DepthRenderBuffer { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public FrameBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        
        // Create framebuffer
        Handle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
        
        // Create color texture
        ColorTexture = new Texture2D(width, height, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.Float);
        ColorTexture.SetFiltering(TextureMinFilter.Linear, TextureMagFilter.Linear);
        ColorTexture.SetWrapMode(TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);
        
        // Attach color texture to framebuffer
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, 
            TextureTarget.Texture2D, ColorTexture.Handle, 0);
        
        // Create depth renderbuffer
        DepthRenderBuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthRenderBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, 
            RenderbufferTarget.Renderbuffer, DepthRenderBuffer);
        
        // Check if framebuffer is complete
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            throw new Exception($"Framebuffer is not complete: {status}");
        }
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        
        var label = $"Framebuffer {width}x{height}";
        GL.ObjectLabel(ObjectLabelIdentifier.Framebuffer, Handle, label.Length, label);
    }

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
        GL.Viewport(0, 0, Width, Height);
    }

    public void Unbind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Resize(int width, int height)
    {
        if (Width == width && Height == height)
            return;

        Width = width;
        Height = height;

        // Resize color texture
        ColorTexture.Resize(width, height);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, 
            TextureTarget.Texture2D, ColorTexture.Handle, 0);

        // Resize depth renderbuffer
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthRenderBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        if (Handle != 0)
        {
            GL.DeleteFramebuffer(Handle);
            Handle = 0;
        }

        if (DepthRenderBuffer != 0)
        {
            GL.DeleteRenderbuffer(DepthRenderBuffer);
            DepthRenderBuffer = 0;
        }

        ColorTexture?.Dispose();
    }
}

