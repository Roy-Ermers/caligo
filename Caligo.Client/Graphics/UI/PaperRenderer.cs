using System.Drawing;
using Caligo.Client.Graphics.Shaders;
using Caligo.Core.FileSystem;
using Caligo.ModuleSystem;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Prowl.PaperUI;
using Prowl.PaperUI.Events;
using Prowl.Quill;
using Prowl.Scribe;
using Prowl.Vector;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace Caligo.Client.Graphics.UI;

using Matrix4x4 = Matrix4x4;

internal class PaperRenderer : ICanvasRenderer
{
    private readonly Texture2D _defaultTexture;
    private readonly int _elementBufferObject;

    private readonly RenderShader _shader;

    // OpenGL objects
    private readonly int _vertexArrayObject;
    private readonly int _vertexBufferObject;

    private readonly NativeWindow _window;

    private bool _cursorSet;

    private Matrix4 _projection;
    public FontFile Font = null!;
    public FontFile IconFont = null!;
    public FontFile MonospaceFont = null!;

    public Action OnFrameEnd = () => { };
    public Action OnFrameStart = () => { };

    public UiStyle Style;

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

        window.Resize += e => UpdateProjection(e.Width, e.Height);
        window.MouseDown += OnMouseDown;
        window.MouseUp += OnMouseUp;
        window.MouseMove += OnMouseMove;
        window.MouseWheel += OnMouseWheel;
        window.KeyDown += OnKeyDown;
        window.KeyUp += OnKeyUp;
        window.TextInput += OnTextInput;
    }

    public static PaperRenderer Current { private set; get; } = null!;

    public Paper Paper { get; }

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
        GL.BufferData(BufferTarget.ArrayBuffer, canvas.Vertices.Count * Vertex.SizeInBytes, canvas.Vertices.ToArray(),
            BufferUsageHint.StreamDraw);

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
        GL.BufferData(BufferTarget.ElementArrayBuffer, canvas.Indices.Count * sizeof(uint), canvas.Indices.ToArray(),
            BufferUsageHint.StreamDraw);

        // Active texture unit for sampling
        GL.ActiveTexture(TextureUnit.Texture0);
        _shader.SetTexture2D("texture0", 0, 0);

        // Draw all draw calls in the canvas
        var indexOffset = 0;
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
            _shader.SetVector4("brushParams", (float)drawCall.Brush.Point1.x, (float)drawCall.Brush.Point1.y,
                (float)drawCall.Brush.Point2.x, (float)drawCall.Brush.Point2.y);
            _shader.SetVector2("brushParams2", (float)drawCall.Brush.CornerRadii, (float)drawCall.Brush.Feather);

            GL.DrawElements(PrimitiveType.Triangles, drawCall.ElementCount, DrawElementsType.UnsignedInt,
                indexOffset * sizeof(uint));
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
        var button = TranslateMouseButton(e.Button);
        Paper.SetPointerState(button, _window.MouseState.X, _window.MouseState.Y, true, false);
    }

    private void OnMouseUp(MouseButtonEventArgs e)
    {
        var button = TranslateMouseButton(e.Button);
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
    ///     Update the projection matrix when the window is resized
    /// </summary>
    public void UpdateProjection(int width, int height)
    {
        Paper.SetResolution(width, height);
        _projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
    }

    private static Matrix4 ToTK(Matrix4x4 mat)
    {
        return new Matrix4(
            (float)mat.M11, (float)mat.M12, (float)mat.M13, (float)mat.M14,
            (float)mat.M21, (float)mat.M22, (float)mat.M23, (float)mat.M24,
            (float)mat.M31, (float)mat.M32, (float)mat.M33, (float)mat.M34,
            (float)mat.M41, (float)mat.M42, (float)mat.M43, (float)mat.M44
        );
    }

    private static Vector4 ToTK(Prowl.Vector.Vector4 v)
    {
        return new Vector4(
            (float)v.x, (float)v.y, (float)v.z, (float)v.w
        );
    }

    private static Vector4 ToTK(Color color)
    {
        return new Vector4(
            color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f
        );
    }

    public void SetCursor(MouseCursor? cursor = null)
    {
        _cursorSet = cursor is not null;

        _window.Cursor = cursor ?? MouseCursor.Default;
    }

    private PaperMouseBtn TranslateMouseButton(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => PaperMouseBtn.Left,
            MouseButton.Right => PaperMouseBtn.Right,
            MouseButton.Middle => PaperMouseBtn.Middle,
            _ => PaperMouseBtn.Unknown
        };
    }

    public PaperKey TranslateKey(Keys tk)
    {
        return tk switch
        {
            Keys.Unknown => PaperKey.Unknown,
            Keys.Space => PaperKey.Space,
            Keys.Apostrophe => PaperKey.Apostrophe,
            Keys.Comma => PaperKey.Comma,
            Keys.Minus => PaperKey.Minus,
            Keys.Period => PaperKey.Period,
            Keys.Slash => PaperKey.Slash,
            Keys.D0 => PaperKey.Num0,
            Keys.D1 => PaperKey.Num1,
            Keys.D2 => PaperKey.Num2,
            Keys.D3 => PaperKey.Num3,
            Keys.D4 => PaperKey.Num4,
            Keys.D5 => PaperKey.Num5,
            Keys.D6 => PaperKey.Num6,
            Keys.D7 => PaperKey.Num7,
            Keys.D8 => PaperKey.Num8,
            Keys.D9 => PaperKey.Num9,
            Keys.Semicolon => PaperKey.Semicolon,
            Keys.Equal => PaperKey.Equals,
            Keys.A => PaperKey.A,
            Keys.B => PaperKey.B,
            Keys.C => PaperKey.C,
            Keys.D => PaperKey.D,
            Keys.E => PaperKey.E,
            Keys.F => PaperKey.F,
            Keys.G => PaperKey.G,
            Keys.H => PaperKey.H,
            Keys.I => PaperKey.I,
            Keys.J => PaperKey.J,
            Keys.K => PaperKey.K,
            Keys.L => PaperKey.L,
            Keys.M => PaperKey.M,
            Keys.N => PaperKey.N,
            Keys.O => PaperKey.O,
            Keys.P => PaperKey.P,
            Keys.Q => PaperKey.Q,
            Keys.R => PaperKey.R,
            Keys.S => PaperKey.S,
            Keys.T => PaperKey.T,
            Keys.U => PaperKey.U,
            Keys.V => PaperKey.V,
            Keys.W => PaperKey.W,
            Keys.X => PaperKey.X,
            Keys.Y => PaperKey.Y,
            Keys.Z => PaperKey.Z,
            Keys.LeftBracket => PaperKey.LeftBracket,
            Keys.Backslash => PaperKey.Backslash,
            Keys.RightBracket => PaperKey.RightBracket,
            Keys.GraveAccent => PaperKey.Grave,
            Keys.Escape => PaperKey.Escape,
            Keys.Enter => PaperKey.Enter,
            Keys.Tab => PaperKey.Tab,
            Keys.Backspace => PaperKey.Backspace,
            Keys.Insert => PaperKey.Insert,
            Keys.Delete => PaperKey.Delete,
            Keys.Right => PaperKey.Right,
            Keys.Left => PaperKey.Left,
            Keys.Down => PaperKey.Down,
            Keys.Up => PaperKey.Up,
            Keys.PageUp => PaperKey.PageUp,
            Keys.PageDown => PaperKey.PageDown,
            Keys.Home => PaperKey.Home,
            Keys.End => PaperKey.End,
            Keys.CapsLock => PaperKey.CapsLock,
            Keys.ScrollLock => PaperKey.ScrollLock,
            Keys.NumLock => PaperKey.NumLock,
            Keys.PrintScreen => PaperKey.PrintScreen,
            Keys.Pause => PaperKey.Pause,
            Keys.F1 => PaperKey.F1,
            Keys.F2 => PaperKey.F2,
            Keys.F3 => PaperKey.F3,
            Keys.F4 => PaperKey.F4,
            Keys.F5 => PaperKey.F5,
            Keys.F6 => PaperKey.F6,
            Keys.F7 => PaperKey.F7,
            Keys.F8 => PaperKey.F8,
            Keys.F9 => PaperKey.F9,
            Keys.F10 => PaperKey.F10,
            Keys.F11 => PaperKey.F11,
            Keys.F12 => PaperKey.F12,
            Keys.KeyPad0 => PaperKey.Keypad0,
            Keys.KeyPad1 => PaperKey.Keypad1,
            Keys.KeyPad2 => PaperKey.Keypad2,
            Keys.KeyPad3 => PaperKey.Keypad3,
            Keys.KeyPad4 => PaperKey.Keypad4,
            Keys.KeyPad5 => PaperKey.Keypad5,
            Keys.KeyPad6 => PaperKey.Keypad6,
            Keys.KeyPad7 => PaperKey.Keypad7,
            Keys.KeyPad8 => PaperKey.Keypad8,
            Keys.KeyPad9 => PaperKey.Keypad9,
            Keys.KeyPadDecimal => PaperKey.KeypadDecimal,
            Keys.KeyPadDivide => PaperKey.KeypadDivide,
            Keys.KeyPadMultiply => PaperKey.KeypadMultiply,
            Keys.KeyPadSubtract => PaperKey.KeypadMinus,
            Keys.KeyPadAdd => PaperKey.KeypadPlus,
            Keys.KeyPadEnter => PaperKey.KeypadEnter,
            Keys.KeyPadEqual => PaperKey.KeypadEquals,
            Keys.LeftShift => PaperKey.LeftShift,
            Keys.LeftControl => PaperKey.LeftControl,
            Keys.LeftAlt => PaperKey.LeftAlt,
            Keys.LeftSuper => PaperKey.LeftSuper,
            Keys.RightShift => PaperKey.RightShift,
            Keys.RightControl => PaperKey.RightControl,
            Keys.RightAlt => PaperKey.RightAlt,
            Keys.RightSuper => PaperKey.RightSuper,
            Keys.Menu => PaperKey.Menu,
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
                element.Data.OnPress?.Invoke(new ClickEvent(element, element.Data.LayoutRect, Paper.PointerPos,
                    PaperMouseBtn.Left));
        }

        PaperRenderer.Current.OnFrameEnd();
        Paper.EndFrame();
    }
}