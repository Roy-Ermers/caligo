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
using WorldGen.Resources.Block;
using WorldGen.Threading;
using WorldGen.Universe;
using WorldGen.Universe.PositionTypes;
using System.Text;
using WorldGen.Renderer.Worlds;
using WorldGen.Generators.World;
using WorldGen.Graphics.UI.PaperComponents;
using WorldGen.Debugging.UI;
using WorldGen.Debugging.UI.Modules;
using OpenTK.Windowing.Common.Input;
using WorldGen.Debugging;
using System.Numerics;
using System.Drawing;

namespace WorldGen;

public class Game : GameWindow
{
    public static Game Instance { get; protected set; }
    public readonly Camera Camera;
    public readonly ModuleRepository ModuleRepository;
    private readonly PaperRenderer uiRenderer;
    private readonly DebugUiRenderer debugUiRenderer;

    private readonly RenderShader _shader;

    private readonly RenderDoc? renderdoc;

    public MainThread? MainThread { get; } = new();
    public World world = null!;
    public WorldBuilder builder = null!;
    public WorldRenderer renderer = null!;

    public List<string> SupportedExtensions { get; private set; } = [];

    public Atlas atlas;

    public double Time = 0;

    public Game() : base(new()
    {
        UpdateFrequency = 60.0
    },
        new() { ClientSize = new(1280, 720), Title = "Voxels", WindowState = WindowState.Maximized })
    {
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
        Instance = this;
        renderdoc = RenderDoc.Load();

        var importer = new ModuleImporter()
            .AddImporter<TextureImporter>()
            .AddImporter<ShaderImporter>()
            .AddImporter<FontImporter>()
            .AddImporter<BlockModelImporter>()
            .AddImporter<BlockImporter>()
            .AddImporter<ConfigImporter>()
            .AddImporter<WindowIconImporter>();

        ModuleRepository = new ModuleRepository(importer);

        ModuleRepository.LoadModules("modules");
        ModuleRepository.Build();
        var icon = ModuleRepository.Get<WindowIcon>("window_icon");

        _shader = ModuleRepository.Get<RenderShader>("default");
        _shader.Initialize();

        atlas = ModuleRepository.Get<Atlas>("block_atlas");
        Camera = new Camera(this);

        MainThread = new MainThread();

        uiRenderer = new(this);

        Gizmo3D.Initialize(this);

        debugUiRenderer = [
            new CameraDebugModule(this),
            new OpenGLDebugModule(),
            new StatsDebugModule(this),
            new ResourcesDebugModule(this),
            new ModuleDebugModule(this),
            new ChunkDebugModule(this)
        ];

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
        GL.ClearColor(0.9f, 0.9f, 1f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.PolygonMode(TriangleFace.Front, PolygonMode.Fill);


        var blockStorage = ModuleRepository.GetAll<Block>();

        world = new World();
        builder = new(world, new LayeredWorldGenerator(0));
        renderer = new WorldRenderer(world, atlas, blockStorage);

        // _uiRenderer = new UiRenderer(this);
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
        // MainThread?.Update();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Time += args.Time;
        world.DebugRender();

        using var shader = _shader.Use();
        _shader.SetMatrix4("camera.projection", Camera.ProjectionMatrix);
        _shader.SetMatrix4("camera.view", Camera.ViewMatrix);
        _shader.SetVector3("camera.position", Camera.Position);
        _shader.SetFloat("uTime", (float)Time);

        renderer.Draw(_shader);

        Camera.Update(args.Time);
        RenderUI(args);
        SwapBuffers();
    }

    void RenderUI(FrameEventArgs args)
    {
        using var uiFrame = uiRenderer.Start(args.Time);
        Gizmo3D.Render();

        Components.Text($"FPS: {(int)(1 / args.Time)}", fontFamily: FontFamily.Monospace).Margin(8);
        debugUiRenderer.Render();
    }


    protected override void OnResize(ResizeEventArgs e)
    {
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
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
