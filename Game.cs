using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using WorldGen.Graphics;
using WorldGen.ModuleSystem;
using WorldGen.ModuleSystem.Importers;
using WorldGen.Resources.Atlas;
using WorldGen.Graphics.UI;
using WorldGen.Graphics.Shaders;
using WorldGen.Debugging.RenderDoc;
using WorldGen.ModuleSystem.Importers.Blocks;
using ImGuiNET;
using OpenTK.Mathematics;
using WorldGen.Resources.Block;
using OpenTK.Windowing.GraphicsLibraryFramework;
using WorldGen.Threading;
using WorldGen.Universe;
using WorldGen.Universe.WorldGenerators;
using WorldGen.Universe.PositionTypes;
using System.Text;
using WorldGen.Renderer.Worlds;

namespace WorldGen;

public class Game : GameWindow
{
    public readonly Camera Camera;
    public readonly ModuleRepository ModuleRepository;
    private UiRenderer _uiRenderer;

    private readonly RenderShader _shader;

    private readonly RenderDoc? renderdoc;

    public MainThread? MainThread { get; } = new();
    public World world = null!;
    public WorldBuilder builder = null!;
    public WorldRenderer renderer = null!;

    public double Time = 0;

    public Game() : base(GameWindowSettings.Default,
        new() { ClientSize = new(1280, 720), Title = "Voxels", WindowState = WindowState.Maximized })
    {
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

        renderdoc = RenderDoc.Load();

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

        _shader = ModuleRepository.Get<RenderShader>("default");
        _shader.Initialize();

        Camera = new Camera(this);

        MainThread = new MainThread();
    }

    private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
        Console.WriteLine("an exception occured");


        if (args.ExceptionObject is not Exception e)
        {
            Console.WriteLine("Exception object is null");
            return;
        }

        string message = e.Message;
        string[] stackTrace = e.StackTrace?.Split(Environment.NewLine) ?? [];

        using FileStream stream = new("CrashReport.txt", FileMode.Create);

        stream.Write(Encoding.UTF8.GetBytes($"{e.GetType()}: {e.Message}\n"));
        stream.Write(stackTrace.Select(static line => Encoding.UTF8.GetBytes(line + '\n')).SelectMany(bytes => bytes).ToArray());

    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.DebugMessageCallback(DebugMessageDelegate, nint.Zero);
        GL.ClearColor(0.2f, 0.2f, 0.4f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.PolygonMode(TriangleFace.Front, PolygonMode.Fill);


        var atlas = ModuleRepository.Get<Atlas>("block_atlas");
        var blockStorage = ModuleRepository.GetAll<Block>();

        world = new World();
        builder = new(world, new HillyWorldGenerator(1));
        renderer = new WorldRenderer(world, atlas, blockStorage);

        _uiRenderer = new UiRenderer(this);
    }

    private void LoadChunksAroundPlayer()
    {
        const int RenderDistance = 5;
        var playerPosition = Camera.Position;
        for (int x = -RenderDistance; x < RenderDistance; x++)
            for (int y = -RenderDistance; y < RenderDistance; y++)
                for (int z = -RenderDistance; z < RenderDistance; z++)
                {
                    var chunkPosition = ChunkPosition.FromWorldPosition(
                        (x * Chunk.Size) + (int)playerPosition.X,
                        (y * Chunk.Size) + (int)playerPosition.Y,
                        (z * Chunk.Size) + (int)playerPosition.Z
                    );

                    var chunkLoader = new ChunkLoader(chunkPosition, 1);
                    world.EnqueueChunk(chunkLoader);
                }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        LoadChunksAroundPlayer();

        builder.Update();
        renderer.Update();
        world.Update();

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

        renderer.Draw(_shader);

        Camera.Update(args.Time);

        RenderUI(args);
        SwapBuffers();
    }

    void RenderUI(FrameEventArgs args)
    {

        using var frame = _uiRenderer.StartFrame(args.Time);

        if (renderdoc is not null)
        {
            if (renderdoc.IsCapturing)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "Capturing renderdoc data...");
            }

            if (KeyboardState.IsKeyPressed(Keys.F12))
            {
                renderdoc.ToggleCapture();
            }
        }

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
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        _uiRenderer.WindowResized(Size.X, Size.Y);
        base.OnResize(e);
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
