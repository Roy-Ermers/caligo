using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using WorldGen.FileSystem.Images;
using WorldGen.Renderer;
using WorldGen.ModuleSystem;
using WorldGen.ModuleSystem.Importers;
using WorldGen.Resources.Atlas;
using WorldGen.Renderer.UI;
using WorldGen.Renderer.Shaders;
using WorldGen.Debugging.RenderDoc;
using WorldGen.ModuleSystem.Importers.Blocks;
using ImGuiNET;
using OpenTK.Mathematics;
using WorldGen.ChunkRenderer;
using WorldGen.Resources.Block;
using WorldGen.ChunkRenderer.Materials;
using WorldGen.WorldGenerator;
using WorldGen.WorldGenerator.Chunks;
using OpenTK.Windowing.GraphicsLibraryFramework;
using WorldGen.Threading;

namespace WorldGen;

public class Game : GameWindow
{
    public readonly Camera Camera;
    public readonly ModuleRepository ModuleRepository;
    private readonly UiRenderer _uiRenderer;

    private readonly RenderShader _shader;

    private readonly RenderDoc? renderdoc;

    public MainThread? MainThread { get; } = new();

    private ChunkRenderer.ChunkRenderer _chunkRenderer = null!;

    public double Time = 0;

    public Game() : base(GameWindowSettings.Default,
        new() { ClientSize = new(1280, 720), Title = "Voxels", WindowState = WindowState.Maximized })
    {
#if DEBUG
        RenderDoc.Load(out var renderDoc);
        if (renderDoc is not null)
        {
            Console.WriteLine($"RenderDoc loaded: {renderDoc.API.GetAPIVersion}");
            renderdoc = renderDoc;
            renderDoc.API.SetCaptureFilePathTemplate("captures/WorldGenCapture");
        }
#endif
        Console.WriteLine(Directory.GetCurrentDirectory());
        var importer = new ModuleImporter()
            .AddImporter<TextureImporter>()
            .AddImporter<ShaderImporter>()
            .AddImporter<FontImporter>()
            .AddImporter<BlockModelImporter>()
            .AddImporter<BlockImporter>()
            .AddImporter<ConfigImporter>();

        ModuleRepository = new ModuleRepository(importer);

        ModuleRepository.LoadModules("modules");

        ModuleRepository.Build();

        _uiRenderer = new UiRenderer(this);

        _shader = ModuleRepository.Get<RenderShader>("default");
        _shader.Initialize();

        Camera = new Camera(this);

        MainThread = new MainThread();
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.DebugMessageCallback(DebugMessageDelegate, nint.Zero);
        GL.ClearColor(0.2f, 0.2f, 0.4f, 1.0f);
        GL.Enable(EnableCap.CullFace);
        GL.PolygonMode(TriangleFace.Front, PolygonMode.Fill);


        var materialBuffer = new MaterialBuffer();
        var atlas = ModuleRepository.Get<Atlas>("block_atlas");
        var blockStorage = ModuleRepository.GetAll<Block>();

        _chunkRenderer = new ChunkRenderer.ChunkRenderer(materialBuffer, atlas, blockStorage);

        var chunk = new Chunk(ChunkPosition.Zero);

        for (short i = 0; i < Chunk.Size * Chunk.Size * Chunk.Size; i++)
        {
            var position = ChunkPosition.FromIndex(i);

            if (position.Y == 2)
                chunk.Set(position, ModuleRepository.Get<Block>("main:grass_block"));
            else if (position.Y < 2)
            {
                chunk.Set(position, ModuleRepository.Get<Block>("main:dirt"));
            }
        }
        // chunk.Set(ChunkPosition.Zero, ModuleRepository.Get<Block>("overlay:overlay_block"));
        // chunk.Set(new ChunkPosition(0, 2, 0), ModuleRepository.Get<Block>("main:debug_block"));

        _chunkRenderer.AddChunk(chunk);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (renderdoc is not null && KeyboardState.IsKeyDown(Keys.F12))
        {
            if (renderdoc.API.IsFrameCapturing() == 1)
            {
                // If we are already capturing, end the capture
                renderdoc.API.EndFrameCapture(IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                // Start a new frame capture
                renderdoc.API.StartFrameCapture(IntPtr.Zero, IntPtr.Zero);
            }
        }

        // Update the main thread actions
        MainThread?.Update();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Time += args.Time;

        using var shader = _shader.Use();
        _shader.SetMatrix4("projection", Camera.ProjectionMatrix);
        _shader.SetMatrix4("view", Camera.ViewMatrix);
        _shader.SetVector3("uCameraPosition", Camera.Position);
        _shader.SetFloat("uTime", (float)Time);
        _chunkRenderer.Draw(_shader);

        Camera.Update(args.Time);

        RenderUI(args);
        SwapBuffers();
    }

    void RenderUI(FrameEventArgs args)
    {

        using var frame = _uiRenderer.StartFrame(args.Time);
        var oldPos = ImGui.GetCursorScreenPos();

        var center = Camera.WorldToScreen(Vector3.Zero);

        var character = "#";
        if (center is not null)
        {
            ImGui.SetCursorScreenPos(new System.Numerics.Vector2(center.Value.X, center.Value.Y));
            ImGui.TextColored(System.Numerics.Vector4.One, "o");
        }

        var unitX = Camera.WorldToScreen(Vector3.UnitX);
        if (unitX is not null)
        {
            ImGui.SetCursorScreenPos(new System.Numerics.Vector2(unitX.Value.X, unitX.Value.Y));
            ImGui.TextColored(System.Numerics.Vector4.UnitX + System.Numerics.Vector4.UnitW, "X");
        }
        var unitY = Camera.WorldToScreen(Vector3.UnitY);
        if (unitY is not null)
        {
            ImGui.SetCursorScreenPos(new System.Numerics.Vector2(unitY.Value.X, unitY.Value.Y));
            ImGui.TextColored(System.Numerics.Vector4.UnitY + System.Numerics.Vector4.UnitW, "Y");
        }

        var unitZ = Camera.WorldToScreen(Vector3.UnitZ);
        if (unitZ is not null)
        {
            ImGui.SetCursorScreenPos(new System.Numerics.Vector2(unitZ.Value.X, unitZ.Value.Y));
            ImGui.TextColored(System.Numerics.Vector4.UnitZ + System.Numerics.Vector4.UnitW, "Z");
        }

        var chunkY = Camera.WorldToScreen(Vector3.UnitY * Chunk.Size);
        if (chunkY is not null)
        {
            ImGui.SetCursorScreenPos(new System.Numerics.Vector2(chunkY.Value.X, chunkY.Value.Y));
            ImGui.TextColored(System.Numerics.Vector4.UnitW, character);
        }
        var blockCenter = Camera.WorldToScreen(Vector3.One * 0.5f);
        if (blockCenter is not null)
        {
            ImGui.SetCursorScreenPos(new System.Numerics.Vector2(blockCenter.Value.X, blockCenter.Value.Y));
            ImGui.TextColored(System.Numerics.Vector4.One, "B");
        }

        ImGui.SetCursorScreenPos(oldPos);

        _uiRenderer.Draw(args.Time);
    }


    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
    }


    private static readonly DebugProc DebugMessageDelegate = OnDebugMessage;

    private static void OnDebugMessage(
        DebugSource source,
        DebugType type,
        int id,
        DebugSeverity severity,
        int length,
        nint pMessage,
        nint pUserParam)
    {
        // In order to access the string pointed to by pMessage, you can use Marshal
        // class to copy its contents to a C# string without unsafe code. You can
        // also use the new function Marshal.PtrToStringUTF8 since .NET Core 1.1.
        var message = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(pMessage, length);

        // The rest of the function is up to you to implement, however a debug output
        // is always useful.
        Console.WriteLine("[{0} source={1} type={2} id={3} userParam={4}] {5}", severity, source, type, id, pUserParam,
            message);

        // Potentially, you may want to throw from the function for certain severity
        // messages.
        if (type == DebugType.DebugTypeError || severity == DebugSeverity.DebugSeverityHigh)
        {
            throw new Exception(message);
        }
    }
}
