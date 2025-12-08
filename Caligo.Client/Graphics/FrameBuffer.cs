using OpenTK.Graphics.OpenGL;

namespace Caligo.Client.Graphics;

public class FrameBuffer : IDisposable
{
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

        // Create depth texture (instead of renderbuffer, so we can sample it)
        DepthTexture = new Texture2D(width, height, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent,
            PixelType.Float);
        DepthTexture.SetFiltering(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
        DepthTexture.SetWrapMode(TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge);

        // Disable depth comparison mode so we can sample raw depth values
        GL.BindTexture(TextureTarget.Texture2D, DepthTexture.Handle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None);

        // Attach depth texture to framebuffer
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, DepthTexture.Handle, 0);

        // Check if framebuffer is complete
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
            throw new Exception($"Framebuffer is not complete: {status}");

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        var label = $"Framebuffer {width}x{height}";
        GL.ObjectLabel(ObjectLabelIdentifier.Framebuffer, Handle, label.Length, label);
    }

    public int Handle { get; private set; }
    public Texture2D ColorTexture { get; }
    public Texture2D DepthTexture { get; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public void Dispose()
    {
        if (Handle != 0)
        {
            GL.DeleteFramebuffer(Handle);
            Handle = 0;
        }

        ColorTexture?.Dispose();
        DepthTexture?.Dispose();
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

        // Resize depth texture
        DepthTexture.Resize(width, height);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, DepthTexture.Handle, 0);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
}