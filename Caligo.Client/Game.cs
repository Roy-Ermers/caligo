using System.Runtime.InteropServices;
using Caligo.Client.Debugging;
using Caligo.Client.Debugging.RenderDoc;
using Caligo.Client.Debugging.UI;
using Caligo.Client.Debugging.UI.Modules;
using Caligo.Client.Generators.Layers;
using Caligo.Client.Generators.World;
using Caligo.Client.Graphics;
using Caligo.Client.Graphics.Shaders;
using Caligo.Client.Graphics.UI;
using Caligo.Client.Graphics.UI.PaperComponents;
using Caligo.Client.ModuleSystem.Importers;
using Caligo.Client.Player;
using Caligo.Client.Renderer;
using Caligo.Client.Renderer.Worlds;
using Caligo.Client.Threading;
using Caligo.Core.ModuleSystem.Importers;
using Caligo.Core.ModuleSystem.Importers.Blocks;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.ModuleSystem;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;
using World = Caligo.Core.Universe.Worlds.World;

namespace Caligo.Client;

public class Game : GameWindow
{
    private static readonly DebugProc DebugMessageDelegate = OnDebugMessage;

    private readonly RenderShader _shader;

    public readonly ModuleRepository ModuleRepository;

    private readonly RenderDoc? renderdoc;
    private readonly PaperRenderer uiRenderer;
    private bool _cursorLocked = true;
    private bool _firstMouseMove = true;

    private Vector2 _lastMousePosition;
    public WorldBuilder builder = null!;

    public PlayerController Controller;
    private DebugUiRenderer debugUiRenderer;
    public WorldRenderer renderer = null!;


    public double Time;
    public World world = null!;

    public Game() : base(new GameWindowSettings(),
        new NativeWindowSettings
            { ClientSize = new Vector2i(1280, 720), Title = "Voxels", WindowState = WindowState.Maximized })
    {
        Instance = this;
        renderdoc = RenderDoc.Load();

        ModuleRepository = new ModuleRepository();

        new ModuleImporter(ModuleRepository)
            .AddImporter<TextureImporter>()
            .AddImporter<ShaderImporter>()
            .AddImporter<FontImporter>()
            .AddImporter<BlockModelImporter>()
            .AddImporter<BlockImporter>()
            .AddImporter<ConfigImporter>()
            .Load("modules");

        _shader = ModuleRepository.Get<RenderShader>("default");
        _shader.Initialize();

        Camera = new Camera(this);
        Camera.Position = Vector3.UnitY * 64f;

        MainThread = new MainThread();

        uiRenderer = new PaperRenderer(this);

        Gizmo3D.Initialize(this);
    }

    public static Game Instance { get; protected set; } = null!;

    public Camera Camera { get; set; }

    public MainThread? MainThread { get; } = new();

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.DebugMessageCallback(DebugMessageDelegate, nint.Zero);
        GL.ClearColor(0.46f, 0.66f, 0.9f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.PolygonMode(TriangleFace.Front, PolygonMode.Fill);

        var blockStorage = ModuleRepository.GetAll<Block>();

        world = new World();
        builder = new WorldBuilder(world, new LayerWorldGenerator(0, world, [
            new HeightLayer(),
            new GroundLayer(),
            new FeatureLayer(),
            new VegetationLayer()
        ]));
        renderer = new WorldRenderer(world, ModuleRepository, blockStorage);
        Controller = new PlayerController(Camera, world);

        debugUiRenderer =
        [
            new CameraDebugModule(this),
            new OpenGLDebugModule(),
            new StatsDebugModule(this),
            new ResourcesDebugModule(this),
            new ModuleDebugModule(this),
            new ChunkDebugModule(this)
        ];

        // Lock cursor for FPS controls
        CursorState = CursorState.Grabbed;
    }

    private void LoadChunksAroundPlayer()
    {
        var RenderDistance = renderer.RenderDistance / 2;

        var playerPosition = Camera.Position;
        for (var x = -RenderDistance; x < RenderDistance; x++)
        for (var y = -RenderDistance; y < RenderDistance; y++)
        for (var z = -RenderDistance; z < RenderDistance; z++)
        {
            var chunkPosition = ChunkPosition.FromWorldPosition(
                x * Chunk.Size + (int)playerPosition.X,
                y * Chunk.Size + (int)playerPosition.Y,
                z * Chunk.Size + (int)playerPosition.Z
            );

            var chunkLoader = new ChunkLoader(chunkPosition);
            world.EnqueueChunk(chunkLoader);
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        LoadChunksAroundPlayer();

        if (KeyboardState.IsKeyPressed(Keys.R))
        {
            world.Clear();
            renderer.Clear();
        }

        // Toggle cursor lock with Escape
        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            _cursorLocked = !_cursorLocked;
            CursorState = _cursorLocked ? CursorState.Grabbed : CursorState.Normal;
            _firstMouseMove = true; // Reset to avoid camera jump when re-locking
        }

        // Mouse look (only when cursor is locked)
        var mouse = MouseState;
        var mousePos = new Vector2(mouse.X, mouse.Y);
        if (_firstMouseMove)
        {
            _lastMousePosition = mousePos;
            _firstMouseMove = false;
        }

        if (_cursorLocked)
        {
            var delta = mousePos - _lastMousePosition;
            Controller.ProcessMouseLook(delta.X, delta.Y);
        }

        _lastMousePosition = mousePos;

        // Update camera so Forward/Right vectors are current before processing movement
        Camera.Update();

        // Movement keys (must be after Camera.Update so Forward/Right are up-to-date)
        var forward = KeyboardState.IsKeyDown(Keys.W);
        var backward = KeyboardState.IsKeyDown(Keys.S);
        var left = KeyboardState.IsKeyDown(Keys.A);
        var right = KeyboardState.IsKeyDown(Keys.D);
        var jump = KeyboardState.IsKeyDown(Keys.Space);
        var sprint = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
        Controller.ProcessMovement(forward, backward, left, right, jump, sprint);
        // --- END PLAYER INPUT HANDLING ---

        builder.Update();
        renderer.Update();
        world.Update();

        Controller.Update(args.Time);
        // Update the main thread actions
        MainThread?.Update();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Time += args.Time;
        FpsMeter.FpsGauge.Record(1.0 / args.Time);

        world.DebugRender();

        using var shader = _shader.Use();
        _shader.SetMatrix4("camera.projection", Camera.ProjectionMatrix);
        _shader.SetMatrix4("camera.view", Camera.ViewMatrix);
        _shader.SetVector3("camera.position", Camera.Position);
        _shader.SetFloat("uTime", (float)Time);

        renderer.Draw(_shader);

        Camera.Update();
        RenderUI(args);
        SwapBuffers();
    }

    private void RenderUI(FrameEventArgs args)
    {
        using var uiFrame = uiRenderer.Start(args.Time);


        Components.Text("+").PositionType(PositionType.SelfDirected).Margin(UnitValue.StretchOne);

        Gizmo3D.Render();

        var hitinfo = uiFrame.Paper.Column("hitinfo")
            .PositionType(PositionType.SelfDirected)
            .Top(0)
            .Left(0)
            .Margin(UnitValue.StretchOne, 8)
            .BackgroundColor(Components.Style.FrameBackground)
            .BorderWidth(1)
            .BorderColor(Components.Style.BorderColor)
            .BoxShadow(0, 2, 4, 0, Components.Style.BorderShadow)
            .Width(UnitValue.Auto)
            .Height(UnitValue.Auto)
            .MinWidth(64)
            .Visible(false)
            .Clip()
            .Border(8)
            .Rounded(8);
        using (hitinfo.Enter())
        {
            if (world.Raycast(Camera.Ray, 5f, out var hit))
            {
                hitinfo.Visible(true);
                Gizmo3D.DrawBoundingBox(new BoundingBox(hit.Position, 1, 1, 1));

                var identifier = Identifier.Parse(hit.Block.Name);

                using (uiFrame.Paper.Row("identifier").Height(UnitValue.Auto).Width(UnitValue.Auto).RowBetween(8)
                           .Enter())
                {
                    Components.Text(identifier.module, 20f)
                        .TextColor(Components.Style.AccentColor).Alignment(TextAlignment.MiddleLeft);
                    Components.Text(identifier.name, 20f)
                        .TextColor(Components.Style.TextColor).Alignment(TextAlignment.MiddleRight);
                }

                Components.Text(
                        $"Variant: {Array.IndexOf(hit.Block.Variants, hit.Block.GetVariant(hit.Position.Id)) + 1}/{hit.Block.Variants.Length}",
                        12f, FontFamily.Monospace)
                    .TextColor(Components.Style.SecondaryTextColor);
            }
        }

        Components.Text($"FPS: {(int)(1 / args.Time)}", fontFamily: FontFamily.Monospace).Margin(8);
        debugUiRenderer.Render();
    }


    protected override void OnResize(ResizeEventArgs e)
    {
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        base.OnResize(e);
    }

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
        var message = Marshal.PtrToStringAnsi(pMessage, length);

        // The rest of the function is up to you to implement, however a debug output
        // is always useful.
        Console.WriteLine("[{0} source={1} type={2} id={3} userParam={4}] {5}", severity, source, type, id, pUserParam,
            message);

        // Potentially, you may want to throw from the function for certain severity
        // messages.
        if (type == DebugType.DebugTypeError || severity == DebugSeverity.DebugSeverityHigh)
            throw new Exception(message);
    }
}