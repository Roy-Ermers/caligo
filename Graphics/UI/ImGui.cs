using System.Diagnostics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using WorldGen.FileSystem;
using WorldGen.Graphics.Shaders;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

namespace WorldGen.Graphics.UI;

public class ImGuiRenderer : IDisposable
{
    const string UiSettingsFileName = "UI.ini";
    private readonly Game _window;
    private bool _frameBegun;

    private int _vertexArray;
    private int _vertexBuffer;
    private int _vertexBufferSize;
    private int _indexBuffer;
    private int _indexBufferSize;

    private int _fontTexture;
    private int _textureArraySliceTexture;
    private (int, int) _currentTextureArraySlice;

    private readonly RenderShader _shader;

    private int _windowWidth;
    private int _windowHeight;

    private System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;

    private static bool _hhrDebugAvailable;

    /// <summary>
    /// Constructs a new ImGuiRenderer.
    /// </summary>
    public ImGuiRenderer(Game window)
    {
        _window = window;
        _windowWidth = window.ClientSize.X;
        _windowHeight = window.ClientSize.Y;

        _shader = _window.ModuleRepository.Get<RenderShader>("ui");
        _shader.Initialize();

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
    }

    public static unsafe void AddFont(Font font, float size = 16f)
    {
        var io = ImGui.GetIO();

        var glyphRange = (ImFontGlyphRangesBuilderPtr)ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder();
        glyphRange.AddRanges(io.Fonts.GetGlyphRangesDefault());
        glyphRange.AddChar('•');
        glyphRange.AddChar('✓');
        glyphRange.AddChar('✗');
        glyphRange.AddChar('←');
        glyphRange.BuildRanges(out var ranges);

        ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
        fontConfig.FontDataOwnedByAtlas = false;
        fontConfig.OversampleH = 3;
        fontConfig.OversampleV = 3;
        fontConfig.RasterizerMultiply = 1.0f;
        fontConfig.GlyphRanges = ranges.Data;
        fontConfig.PixelSnapH = true;
        fontConfig.GlyphMaxAdvanceX = float.MaxValue;

        io.Fonts.AddFontFromFileTTF(font.FilePath, size, fontConfig, ranges.Data);
    }

    public static string LoadSettings()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), UiSettingsFileName);
        if (!File.Exists(path))
            return string.Empty;

        return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), UiSettingsFileName));
    }

    public unsafe static void SaveSettings()
    {
        var settings = ImGui.SaveIniSettingsToMemory();
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), UiSettingsFileName), settings);
    }

    public void Initialize()
    {
        _window.TextInput += (args) => { PressChar((char)args.Unicode); };

        _window.MouseWheel += (args) => { MouseScroll(args.Offset); };

        _window.Resize += (_) => { WindowResized(_window.ClientSize.X, _window.ClientSize.Y); };


        var major = GL.GetInteger(GetPName.MajorVersion);
        var minor = GL.GetInteger(GetPName.MinorVersion);

        _hhrDebugAvailable = (major == 4 && minor >= 3) || IsExtensionSupported("KHR_debug");

        var io = ImGui.GetIO();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.HasMouseCursors |
                           ImGuiBackendFlags.PlatformHasViewports | ImGuiBackendFlags.HasMouseHoveredViewport;
        // Enable Docking
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.DpiEnableScaleViewports | ImGuiConfigFlags.DpiEnableScaleFonts;
        unsafe
        {
            io.NativePtr->IniFilename = null;
        }
        ImGui.LoadIniSettingsFromMemory(LoadSettings());

        CreateDeviceResources();

        SetPerFrameImGuiData(1f / 60f);

        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void DestroyDeviceObjects()
    {
        Dispose();
    }

    public void CreateDeviceResources()
    {
        _vertexBufferSize = 10000;
        _indexBufferSize = 2000;

        var prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
        var prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);

        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);
        LabelObject(ObjectLabelIdentifier.VertexArray, _vertexArray, "ImGui");

        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        LabelObject(ObjectLabelIdentifier.Buffer, _vertexBuffer, "VBO: ImGui");
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        LabelObject(ObjectLabelIdentifier.Buffer, _indexBuffer, "EBO: ImGui");
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        RecreateFontDeviceTexture();

        _textureArraySliceTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _textureArraySliceTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 128, 128, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        LabelObject(ObjectLabelIdentifier.Texture, _textureArraySliceTexture, "ImGui Texture Array Slice");

        var stride = Unsafe.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(prevVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);

        CheckGlError("End of ImGui setup");
    }

    /// <summary>
    /// Recreates the device texture used to render text.
    /// </summary>
    private void RecreateFontDeviceTexture()
    {
        var io = ImGui.GetIO();

        unsafe
        {
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var width, out var height, out var _);

            var mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

            var prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            var prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

            _fontTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
            GL.TexStorage2D(TextureTarget2d.Texture2D, mips, SizedInternalFormat.Rgba8, width, height);
            LabelObject(ObjectLabelIdentifier.Texture, _fontTexture, "ImGui Text Atlas");

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte,
                (IntPtr)pixels);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mips - 1);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

            // Restore state
            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);

            io.Fonts.SetTexID(_fontTexture);
            io.Fonts.ClearTexData();
        }
    }

    public int BindTextureArray(Texture2DArray textureArray, int slice, int unit = 1)
    {
        var (currentTextureArray, currentSlice) = _currentTextureArraySlice;

        if (currentTextureArray == textureArray.Handle && currentSlice == slice)
        {
            return _textureArraySliceTexture;
        }

        _currentTextureArraySlice = (textureArray.Handle, slice);

        GL.ActiveTexture(TextureUnit.Texture0 + unit);
        GL.BindTexture(TextureTarget.Texture2D, _textureArraySliceTexture);

        // resize _textureArraySliceTexture to the size of the texture array
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, textureArray.Width, textureArray.Height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

        textureArray.Bind(unit + 1);
        // copy over the texture array slice over to _textureArraySliceTexture, using GL.copyImageSubData
        GL.CopyImageSubData(
            textureArray.Handle,
            ImageTarget.Texture2DArray,
            0,
            0, 0, slice,
            _textureArraySliceTexture,
            ImageTarget.Texture2D,
            0,
            0, 0, 0,
            textureArray.Width,
            textureArray.Height, 1
        );

        CheckGlError("CopyImageSubData");


        return _textureArraySliceTexture;
    }

    public void Begin(double deltaSeconds)
    {
        Begin((float)deltaSeconds);
    }

    public void Begin(float deltaSeconds)
    {
        Update(deltaSeconds);
    }

    public void End()
    {
        Render();
        CheckGlError("End of frame");
    }

    /// <summary>
    /// Renders the ImGui draw list data.
    /// </summary>
    public void Render()
    {
        if (!_frameBegun)
            return;

        _frameBegun = false;
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());
    }

    /// <summary>
    /// Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(float deltaSeconds)
    {
        if (_frameBegun)
            ImGui.Render();

        SetPerFrameImGuiData(deltaSeconds);
        UpdateImGuiInput();

        _frameBegun = true;
        ImGui.NewFrame();
    }

    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        var io = ImGuiNET.ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(
            _windowWidth / _scaleFactor.X,
            _windowHeight / _scaleFactor.Y);
        io.DisplayFramebufferScale = _scaleFactor;

        if (io.WantSaveIniSettings)
        {
            SaveSettings();
        }
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    readonly List<char> _pressedChars = [];

    private void UpdateImGuiInput()
    {
        var io = ImGui.GetIO();

        var mouseState = _window.MouseState;
        var keyboardState = _window.KeyboardState;

        io.MouseDown[0] = mouseState[MouseButton.Left];
        io.MouseDown[1] = mouseState[MouseButton.Right];
        io.MouseDown[2] = mouseState[MouseButton.Middle];
        io.MouseDown[3] = mouseState[MouseButton.Button4];
        io.MouseDown[4] = mouseState[MouseButton.Button5];

        var screenPoint = new Vector2i((int)mouseState.X, (int)mouseState.Y);
        var point = screenPoint;
        io.MousePos = new System.Numerics.Vector2(point.X, point.Y);

        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            if (key == Keys.Unknown)
            {
                continue;
            }

            io.AddKeyEvent(TranslateKey(key), keyboardState.IsKeyDown(key));
        }

        foreach (var c in _pressedChars)
        {
            io.AddInputCharacter(c);
        }

        _pressedChars.Clear();

        io.KeyCtrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        io.KeyAlt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
        io.KeyShift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        io.KeySuper = keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper);
    }

    internal void PressChar(char keyChar)
    {
        _pressedChars.Add(keyChar);
    }

    internal static void MouseScroll(Vector2 offset)
    {
        var io = ImGui.GetIO();

        io.MouseWheel = offset.Y;
        io.MouseWheelH = offset.X;
    }

    private void RenderImDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0)
            return;
        int framebufferWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
        int framebufferHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
        if (framebufferWidth <= 0 || framebufferHeight <= 0)
            return;
        // Get intial state.
        var prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
        var prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);
        var prevProgram = GL.GetInteger(GetPName.CurrentProgram);
        var prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
        var prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);
        var prevBlendEquationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
        var prevBlendEquationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);
        var prevBlendFuncSrcRgb = GL.GetInteger(GetPName.BlendSrcRgb);
        var prevBlendFuncSrcAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
        var prevBlendFuncDstRgb = GL.GetInteger(GetPName.BlendDstRgb);
        var prevBlendFuncDstAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
        var prevCullFaceEnabled = GL.GetBoolean(GetPName.CullFace);
        var prevDepthTestEnabled = GL.GetBoolean(GetPName.DepthTest);
        var prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        var prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);
        Span<int> prevScissorBox = stackalloc int[4];
        unsafe
        {
            fixed (int* iptr = &prevScissorBox[0])
            {
                GL.GetInteger(GetPName.ScissorBox, iptr);
            }
        }

        Span<int> prevPolygonMode = stackalloc int[2];
        unsafe
        {
            fixed (int* iptr = &prevPolygonMode[0])
            {
                GL.GetInteger(GetPName.PolygonMode, iptr);
            }
        }


        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        // Bind the element buffer (thru the VAO) so that we can resize it.
        GL.BindVertexArray(_vertexArray);
        // Bind the vertex buffer so that we can resize it.
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdLists[i];

            var vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > _vertexBufferSize)
            {
                var newVertexSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);

                GL.BufferData(BufferTarget.ArrayBuffer, newVertexSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _vertexBufferSize = newVertexSize;
            }

            var indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize <= _indexBufferSize) continue;

            var newIndexSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
            GL.BufferData(BufferTarget.ElementArrayBuffer, newIndexSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            _indexBufferSize = newIndexSize;
        }


        // Setup orthographic projection matrix into our constant buffer
        var io = ImGui.GetIO();
        Matrix4 matrix = Matrix4.CreateOrthographicOffCenter(
             0.0f,
             io.DisplaySize.X,
             io.DisplaySize.Y,
             0.0f,
             -1.0f,
             1.0f);

        _shader.Use();
        _shader.SetMatrix4("projection_matrix", matrix);
        _shader.SetTexture2D("in_fontTexture", 0, _fontTexture);

        CheckGlError("Projection");

        GL.BindVertexArray(_vertexArray);
        CheckGlError("VAO");

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        // Will project scissor/clipping rectangles into framebuffer space
        System.Numerics.Vector2 clipOff = drawData.DisplayPos;         // (0,0) unless using multi-viewports
        System.Numerics.Vector2 clipScale = drawData.FramebufferScale; // (1,1) unless using retina display which are often (2,2)
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdListPtr = drawData.CmdLists[n];
            unsafe
            {
                // Upload vertex/index buffers

                GL.BufferData(BufferTarget.ArrayBuffer, (int)(cmdListPtr.VtxBuffer.Size * sizeof(ImDrawVert)), (nint)cmdListPtr.VtxBuffer.Data, BufferUsageHint.StreamDraw);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (int)(cmdListPtr.IdxBuffer.Size * sizeof(ushort)), (nint)cmdListPtr.IdxBuffer.Data, BufferUsageHint.StreamDraw);

                for (int cmd_i = 0; cmd_i < cmdListPtr.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr cmdPtr = cmdListPtr.CmdBuffer[cmd_i];

                    if (cmdPtr.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        Vector4 clipRect;
                        clipRect.X = (cmdPtr.ClipRect.X - clipOff.X) * clipScale.X;
                        clipRect.Y = (cmdPtr.ClipRect.Y - clipOff.Y) * clipScale.Y;
                        clipRect.Z = (cmdPtr.ClipRect.Z - clipOff.X) * clipScale.X;
                        clipRect.W = (cmdPtr.ClipRect.W - clipOff.Y) * clipScale.Y;

                        if (clipRect.X < framebufferWidth && clipRect.Y < framebufferHeight && clipRect.Z >= 0.0f && clipRect.W >= 0.0f)
                        {
                            // Apply scissor/clipping rectangle
                            GL.Scissor((int)clipRect.X, (int)(framebufferHeight - clipRect.W), (int)(clipRect.Z - clipRect.X), (int)(clipRect.W - clipRect.Y));

                            // Bind texture, Draw
                            GL.BindTexture(TextureTarget.Texture2D, (uint)cmdPtr.TextureId);

                            GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)cmdPtr.ElemCount, DrawElementsType.UnsignedShort, (nint)(cmdPtr.IdxOffset * sizeof(ushort)), (int)cmdPtr.VtxOffset);
                        }
                    }
                }
            }
        }

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ScissorTest);

        // Reset state
        GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);
        GL.UseProgram(prevProgram);
        GL.BindVertexArray(prevVAO);
        GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
        GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb, (BlendEquationMode)prevBlendEquationAlpha);
        GL.BlendFuncSeparate(
            (BlendingFactorSrc)prevBlendFuncSrcRgb,
            (BlendingFactorDest)prevBlendFuncDstRgb,
            (BlendingFactorSrc)prevBlendFuncSrcAlpha,
            (BlendingFactorDest)prevBlendFuncDstAlpha);
        if (prevBlendEnabled) GL.Enable(EnableCap.Blend);
        else GL.Disable(EnableCap.Blend);
        if (prevDepthTestEnabled) GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);
        if (prevCullFaceEnabled) GL.Enable(EnableCap.CullFace);
        else GL.Disable(EnableCap.CullFace);
        if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest);
        else GL.Disable(EnableCap.ScissorTest);

        GL.PolygonMode(TriangleFace.FrontAndBack, (PolygonMode)prevPolygonMode[0]);
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);

        GL.DeleteTexture(_fontTexture);
        _shader.Dispose();
    }

    public static void LabelObject(ObjectLabelIdentifier objLabelIdent, int glObject, string name)
    {
        if (_hhrDebugAvailable)
            GL.ObjectLabel(objLabelIdent, glObject, name.Length, name);
    }

    private static bool IsExtensionSupported(string name)
    {
        var n = GL.GetInteger(GetPName.NumExtensions);
        for (var i = 0; i < n; i++)
        {
            var extension = GL.GetString(StringNameIndexed.Extensions, i);
            if (extension == name) return true;
        }

        return false;
    }

    protected static void CheckGlError(string title)
    {
        ErrorCode error;
        var i = 1;
        while ((error = GL.GetError()) != ErrorCode.NoError)
        {
            Debug.Print($"{title} ({i++}): {error}");
        }
    }

    public static ImGuiKey TranslateKey(Keys key)
    {
        return key switch
        {
            >= Keys.D0 and <= Keys.D9 => key - Keys.D0 + ImGuiKey._0,
            >= Keys.A and <= Keys.Z => key - Keys.A + ImGuiKey.A,
            >= Keys.KeyPad0 and <= Keys.KeyPad9 => key - Keys.KeyPad0 + ImGuiKey.Keypad0,
            >= Keys.F1 and <= Keys.F24 => key - Keys.F1 + ImGuiKey.F24,
            Keys.Tab => ImGuiKey.Tab,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.Home => ImGuiKey.Home,
            Keys.End => ImGuiKey.End,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            Keys.Backspace => ImGuiKey.Backspace,
            Keys.Space => ImGuiKey.Space,
            Keys.Enter => ImGuiKey.Enter,
            Keys.Escape => ImGuiKey.Escape,
            Keys.Apostrophe => ImGuiKey.Apostrophe,
            Keys.Comma => ImGuiKey.Comma,
            Keys.Minus => ImGuiKey.Minus,
            Keys.Period => ImGuiKey.Period,
            Keys.Slash => ImGuiKey.Slash,
            Keys.Semicolon => ImGuiKey.Semicolon,
            Keys.Equal => ImGuiKey.Equal,
            Keys.LeftBracket => ImGuiKey.LeftBracket,
            Keys.Backslash => ImGuiKey.Backslash,
            Keys.RightBracket => ImGuiKey.RightBracket,
            Keys.GraveAccent => ImGuiKey.GraveAccent,
            Keys.CapsLock => ImGuiKey.CapsLock,
            Keys.ScrollLock => ImGuiKey.ScrollLock,
            Keys.NumLock => ImGuiKey.NumLock,
            Keys.PrintScreen => ImGuiKey.PrintScreen,
            Keys.Pause => ImGuiKey.Pause,
            Keys.KeyPadDecimal => ImGuiKey.KeypadDecimal,
            Keys.KeyPadDivide => ImGuiKey.KeypadDivide,
            Keys.KeyPadMultiply => ImGuiKey.KeypadMultiply,
            Keys.KeyPadSubtract => ImGuiKey.KeypadSubtract,
            Keys.KeyPadAdd => ImGuiKey.KeypadAdd,
            Keys.KeyPadEnter => ImGuiKey.KeypadEnter,
            Keys.KeyPadEqual => ImGuiKey.KeypadEqual,
            Keys.LeftShift => ImGuiKey.LeftShift,
            Keys.LeftControl => ImGuiKey.LeftCtrl,
            Keys.LeftAlt => ImGuiKey.LeftAlt,
            Keys.LeftSuper => ImGuiKey.LeftSuper,
            Keys.RightShift => ImGuiKey.RightShift,
            Keys.RightControl => ImGuiKey.RightCtrl,
            Keys.RightAlt => ImGuiKey.RightAlt,
            Keys.RightSuper => ImGuiKey.RightSuper,
            Keys.Menu => ImGuiKey.Menu,
            _ => ImGuiKey.None,
        };
    }
}
