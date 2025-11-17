using System.Drawing;
using Caligo.Client.Graphics.Shaders;
using Caligo.Core.FileSystem;
using Caligo.Core.ModuleSystem;
using Caligo.ModuleSystem;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using Prowl.PaperUI;
using Prowl.PaperUI.Events;
using Prowl.Quill;
using Prowl.Scribe;
using Prowl.Vector;
namespace Caligo.Client.Graphics.UI;

using Matrix4x4 = Prowl.Vector.Matrix4x4;

internal class PaperRenderer : Prowl.Quill.ICanvasRenderer
{
    public static PaperRenderer Current { private set; get; } = null!;

    public Paper Paper { private set; get; }

    public UiStyle Style;

    private readonly RenderShader _shader;
    public FontFile Font = null!;
    public FontFile MonospaceFont = null!;
    public FontFile IconFont = null!;

    // OpenGL objects
    private readonly int _vertexArrayObject;
    private readonly int _vertexBufferObject;
    private readonly int _elementBufferObject;

    private Matrix4 _projection;
    private readonly Texture2D _defaultTexture;

    private readonly NativeWindow _window;

    public Action OnFrameEnd = () => { };
    public Action OnFrameStart = () => { };

    public PaperRenderer(NativeWindow window)
    {
        if (Current != null)
            throw new InvalidOperationException("Only one instance of PaperRenderer can be created.");

        Current = this;
        _window = window;

        _shader = ModuleRepository.Current.Get<RenderShader>("paper");
        _shader.Initialize();

        Paper = new Paper(this, window.ClientSize.X, window.ClientSize.Y, new FontAtlasSettings());
        ReadConfig();

        // Create OpenGL buffer objects
        _vertexArrayObject = GL.GenVertexArray();
        _vertexBufferObject = GL.GenBuffer();
        _elementBufferObject = GL.GenBuffer();

        // Set the default texture
        var texture = Texture2D.FromData(1, 1, []);
        byte[] pixelData = [255, 255, 255, 255];
        texture.SetData(pixelData);
        _defaultTexture = texture;

        UpdateProjection(window.ClientSize.X, window.ClientSize.Y);

        OnFrameStart += () =>
        {
            if (!_cursorSet)
                SetCursor();
            _cursorSet = false;
        };

        window.Resize += (e) => UpdateProjection(e.Width, e.Height);
        window.MouseDown += OnMouseDown;
        window.MouseUp += OnMouseUp;
        window.MouseMove += OnMouseMove;
        window.MouseWheel += OnMouseWheel;
        window.KeyDown += OnKeyDown;
        window.KeyUp += OnKeyUp;
        window.TextInput += OnTextInput;
    }

    public void ReadConfig()
    {
        var config = ModuleRepository.Current.GetAll<string>("Config");
        var font = ModuleRepository.Current.Get<Font>(config["ui.font"]);
        var monospaceFont = ModuleRepository.Current.Get<Font>(config["ui.monospaceFont"]);
        var iconFont = ModuleRepository.Current.Get<Font>(config["ui.iconFont"]);

        Font = new FontFile(font.FilePath);
        MonospaceFont = new FontFile(monospaceFont.FilePath);
        IconFont = new FontFile(iconFont.FilePath);

        Paper.AddFallbackFont(IconFont);

        Style = UiStyle.FromConfig(config);
    }

    public PaperUiFrame Start(double time)
    {
        return new PaperUiFrame(Paper, Font, time);
    }

    private void OnMouseDown(MouseButtonEventArgs e)
    {
        PaperMouseBtn button = TranslateMouseButton(e.Button);
        Paper.SetPointerState(button, _window.MouseState.X, _window.MouseState.Y, true, false);
    }

    private void OnMouseUp(MouseButtonEventArgs e)
    {
        PaperMouseBtn button = TranslateMouseButton(e.Button);
        Paper.SetPointerState(button, _window.MouseState.X, _window.MouseState.Y, false, false);
    }
    private void OnMouseMove(MouseMoveEventArgs _)
    {
        Paper.SetPointerState(PaperMouseBtn.Unknown, _window.MouseState.X, _window.MouseState.Y, false, true);
    }

    private void OnMouseWheel(MouseWheelEventArgs e)
    {
        Paper.SetPointerWheel(e.OffsetY);
    }

    private void OnKeyDown(KeyboardKeyEventArgs e)
    {
        Paper.SetKeyState(TranslateKey(e.Key), true);
    }

    private void OnKeyUp(KeyboardKeyEventArgs e)
    {
        Paper.SetKeyState(TranslateKey(e.Key), false);
    }

    private void OnTextInput(TextInputEventArgs e)
    {
        Paper.AddInputCharacter(e.AsString);
    }

    /// <summary>
    /// Update the projection matrix when the window is resized
    /// </summary>
    public void UpdateProjection(int width, int height)
    {
        Console.WriteLine($"Updating projection to {width}x{height}");
        Paper.SetResolution(width, height);
        _projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
    }

    private static Matrix4 ToTK(Matrix4x4 mat) => new(
        (float)mat.M11, (float)mat.M12, (float)mat.M13, (float)mat.M14,
        (float)mat.M21, (float)mat.M22, (float)mat.M23, (float)mat.M24,
        (float)mat.M31, (float)mat.M32, (float)mat.M33, (float)mat.M34,
        (float)mat.M41, (float)mat.M42, (float)mat.M43, (float)mat.M44
    );

    private static OpenTK.Mathematics.Vector4 ToTK(Prowl.Vector.Vector4 v) => new(
        (float)v.x, (float)v.y, (float)v.z, (float)v.w
    );

    private static OpenTK.Mathematics.Vector4 ToTK(Color color) => new(
        color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f
    );

    public object CreateTexture(uint width, uint height)
    {
        var texture = Texture2D.FromData((int)width, (int)height, []);
        texture.MinFilter = TextureMinFilter.Nearest;
        texture.MagFilter = TextureMagFilter.Nearest;

        texture.WrapS = TextureWrapMode.ClampToEdge;
        texture.WrapT = TextureWrapMode.ClampToEdge;
        return texture;
    }

    public Vector2Int GetTextureSize(object texture)
    {
        if (texture is not Texture2D texture2D)
            throw new ArgumentException("Invalid texture type");

        return new Vector2Int(texture2D.Width, texture2D.Height);
    }

    public void SetTextureData(object texture, IntRect bounds, byte[] data)
    {
        if (texture is not Texture2D texture2D)
            throw new ArgumentException("Invalid texture type");
        texture2D.SetRectangle(new Rectangle(bounds.x, bounds.y, bounds.width, bounds.height), data);
    }

    public void RenderCalls(Canvas canvas, IReadOnlyList<DrawCall> drawCalls)
    {
        // Skip if canvas is empty
        if (drawCalls.Count == 0)
            return;

        var oldBlendFunc = GL.GetInteger(GetPName.BlendSrc);
        var oldBlendFuncDst = GL.GetInteger(GetPName.BlendDst);

        // Configure OpenGL state
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

        _shader.Use();
        _shader.SetMatrix4("projection", _projection);

        // Bind vertex array
        GL.BindVertexArray(_vertexArrayObject);

        // Upload vertex data
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, canvas.Vertices.Count * Vertex.SizeInBytes, canvas.Vertices.ToArray(), BufferUsageHint.StreamDraw);

        // Set up vertex attributes
        // Position attribute
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 0);

        // TexCoord attribute
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex.SizeInBytes, 2 * sizeof(float));

        // Color attribute
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, Vertex.SizeInBytes, 4 * sizeof(float));

        // Upload index data
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, canvas.Indices.Count * sizeof(uint), canvas.Indices.ToArray(), BufferUsageHint.StreamDraw);

        // Active texture unit for sampling
        GL.ActiveTexture(TextureUnit.Texture0);
        _shader.SetTexture2D("texture0", 0, 0);

        // Draw all draw calls in the canvas
        int indexOffset = 0;
        foreach (var drawCall in drawCalls)
        {
            // Handle texture binding
            (drawCall.Texture as Texture2D ?? _defaultTexture).Bind(0);

            // Set scissor rectangle
            drawCall.GetScissor(out var scissor, out var extent);
            var tkScissor = ToTK(scissor);
            _shader.SetMatrix4("scissorMat", tkScissor);
            _shader.SetVector2("scissorExt", (float)extent.x, (float)extent.y);

            // Set brush parameters
            var brushMat = ToTK(drawCall.Brush.BrushMatrix);
            _shader.SetMatrix4("brushMat", brushMat);
            _shader.SetInt("brushType", (int)drawCall.Brush.Type);
            _shader.SetColor("brushColor1", drawCall.Brush.Color1);
            _shader.SetColor("brushColor2", drawCall.Brush.Color2);
            _shader.SetVector4("brushParams", (float)drawCall.Brush.Point1.x, (float)drawCall.Brush.Point1.y, (float)drawCall.Brush.Point2.x, (float)drawCall.Brush.Point2.y);
            _shader.SetVector2("brushParams2", (float)drawCall.Brush.CornerRadii, (float)drawCall.Brush.Feather);

            GL.DrawElements(PrimitiveType.Triangles, drawCall.ElementCount, DrawElementsType.UnsignedInt, indexOffset * sizeof(uint));
            indexOffset += drawCall.ElementCount;
        }

        // Clean up
        GL.Enable(EnableCap.DepthTest);
        GL.BlendFunc((BlendingFactor)oldBlendFunc, (BlendingFactor)oldBlendFuncDst);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        // Dispose of OpenGL resources
        GL.DeleteBuffer(_vertexBufferObject);
        GL.DeleteBuffer(_elementBufferObject);
        GL.DeleteVertexArray(_vertexArrayObject);
        _shader.Dispose();
        // Dispose of the default texture
        _defaultTexture?.Dispose();
    }

    bool _cursorSet = false;
    public void SetCursor(MouseCursor? cursor = null)
    {
        _cursorSet = cursor is not null;

        _window.Cursor = cursor ?? MouseCursor.Default;
    }

    private PaperMouseBtn TranslateMouseButton(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton button)
    {
        return button switch
        {
            OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left => PaperMouseBtn.Left,
            OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right => PaperMouseBtn.Right,
            OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle => PaperMouseBtn.Middle,
            _ => PaperMouseBtn.Unknown
        };
    }

    public PaperKey TranslateKey(OpenTK.Windowing.GraphicsLibraryFramework.Keys tk)
    {
        return tk switch
        {
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Unknown => PaperKey.Unknown,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space => PaperKey.Space,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Apostrophe => PaperKey.Apostrophe,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Comma => PaperKey.Comma,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Minus => PaperKey.Minus,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Period => PaperKey.Period,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Slash => PaperKey.Slash,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D0 => PaperKey.Num0,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D1 => PaperKey.Num1,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D2 => PaperKey.Num2,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D3 => PaperKey.Num3,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D4 => PaperKey.Num4,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D5 => PaperKey.Num5,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D6 => PaperKey.Num6,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D7 => PaperKey.Num7,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D8 => PaperKey.Num8,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D9 => PaperKey.Num9,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Semicolon => PaperKey.Semicolon,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Equal => PaperKey.Equals,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.A => PaperKey.A,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.B => PaperKey.B,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.C => PaperKey.C,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.D => PaperKey.D,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.E => PaperKey.E,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F => PaperKey.F,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.G => PaperKey.G,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.H => PaperKey.H,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.I => PaperKey.I,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.J => PaperKey.J,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.K => PaperKey.K,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.L => PaperKey.L,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.M => PaperKey.M,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.N => PaperKey.N,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.O => PaperKey.O,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.P => PaperKey.P,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Q => PaperKey.Q,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.R => PaperKey.R,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.S => PaperKey.S,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.T => PaperKey.T,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.U => PaperKey.U,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.V => PaperKey.V,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.W => PaperKey.W,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.X => PaperKey.X,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Y => PaperKey.Y,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Z => PaperKey.Z,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftBracket => PaperKey.LeftBracket,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backslash => PaperKey.Backslash,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightBracket => PaperKey.RightBracket,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.GraveAccent => PaperKey.Grave,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape => PaperKey.Escape,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter => PaperKey.Enter,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Tab => PaperKey.Tab,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backspace => PaperKey.Backspace,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Insert => PaperKey.Insert,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Delete => PaperKey.Delete,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right => PaperKey.Right,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left => PaperKey.Left,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down => PaperKey.Down,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up => PaperKey.Up,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.PageUp => PaperKey.PageUp,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.PageDown => PaperKey.PageDown,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Home => PaperKey.Home,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.End => PaperKey.End,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.CapsLock => PaperKey.CapsLock,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.ScrollLock => PaperKey.ScrollLock,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.NumLock => PaperKey.NumLock,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.PrintScreen => PaperKey.PrintScreen,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Pause => PaperKey.Pause,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F1 => PaperKey.F1,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F2 => PaperKey.F2,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F3 => PaperKey.F3,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F4 => PaperKey.F4,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F5 => PaperKey.F5,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F6 => PaperKey.F6,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F7 => PaperKey.F7,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F8 => PaperKey.F8,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F9 => PaperKey.F9,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F10 => PaperKey.F10,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F11 => PaperKey.F11,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.F12 => PaperKey.F12,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPad0 => PaperKey.Keypad0,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPad1 => PaperKey.Keypad1,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPad2 => PaperKey.Keypad2,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPad3 => PaperKey.Keypad3,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPad4 => PaperKey.Keypad4,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPad5 => PaperKey.Keypad5,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPad6 => PaperKey.Keypad6,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPad7 => PaperKey.Keypad7,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPad8 => PaperKey.Keypad8,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPad9 => PaperKey.Keypad9,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPadDecimal => PaperKey.KeypadDecimal,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPadDivide => PaperKey.KeypadDivide,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPadMultiply => PaperKey.KeypadMultiply,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPadSubtract => PaperKey.KeypadMinus,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPadAdd => PaperKey.KeypadPlus,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPadEnter => PaperKey.KeypadEnter,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPadEqual => PaperKey.KeypadEquals,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift => PaperKey.LeftShift,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftControl => PaperKey.LeftControl,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftAlt => PaperKey.LeftAlt,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftSuper => PaperKey.LeftSuper,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightShift => PaperKey.RightShift,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightControl => PaperKey.RightControl,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightAlt => PaperKey.RightAlt,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightSuper => PaperKey.RightSuper,
            OpenTK.Windowing.GraphicsLibraryFramework.Keys.Menu => PaperKey.Menu,
            _ => PaperKey.Unknown
        };
    }
}


public ref struct PaperUiFrame : IDisposable
{
    public readonly Paper Paper;
    public readonly FontFile Font;
    public PaperUiFrame(Paper _paper, FontFile font, double time)
    {
        Paper = _paper;
        Font = font;
        Paper.BeginFrame(time);
        PaperRenderer.Current.OnFrameStart();
    }

    public readonly void Dispose()
    {

        var focusedId = Paper.FocusedElementId;
        if (Paper.IsKeyPressed(PaperKey.Space) && Paper.IsElementFocused(focusedId))
        {
            var element = Paper.FindElementByID(focusedId);

            if (element.IsValid)
                element.Data.OnPress?.Invoke(new ClickEvent(element, element.Data.LayoutRect, Paper.PointerPos, PaperMouseBtn.Left));
        }
        PaperRenderer.Current.OnFrameEnd();
        Paper.EndFrame();
    }
}
