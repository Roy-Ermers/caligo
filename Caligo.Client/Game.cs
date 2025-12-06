using System.Text;
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
using Caligo.Client.Renderer;
using Caligo.Client.Renderer.Worlds;
using Caligo.Client.Resources.Atlas;
using Caligo.Client.Threading;
using Caligo.Core.Generators.World;
using Caligo.Core.ModuleSystem;
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
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;
using World = Caligo.Core.Universe.Worlds.World;

namespace Caligo.Client;

public class Game : GameWindow
{
    public static Game Instance { get; protected set; } = null!;
    public readonly Camera Camera;
    public readonly ModuleRepository ModuleRepository;
    private readonly PaperRenderer uiRenderer;
    private DebugUiRenderer debugUiRenderer;

    private readonly RenderShader _shader;

    private readonly RenderDoc? renderdoc;

    public MainThread? MainThread { get; } = new();
    public World world = null!;
    public WorldBuilder builder = null!;
    public WorldRenderer renderer = null!;


    public double Time;

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

        MainThread = new MainThread();

        uiRenderer = new PaperRenderer(this);

        Gizmo3D.Initialize(this);
    }

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

        debugUiRenderer =
        [
            new CameraDebugModule(this),
            new OpenGLDebugModule(),
            new StatsDebugModule(this),
            new ResourcesDebugModule(this),
            new ModuleDebugModule(this),
            new ChunkDebugModule(this)
        ];
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

        if (KeyboardState.IsKeyPressed(Keys.R))
        {
            world.Clear();
            renderer.Clear();
        }

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
        FpsMeter.FpsGauge.Record(1.0 / args.Time);

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


        Components.Text("+").PositionType(PositionType.SelfDirected).Margin(UnitValue.StretchOne);
        
        Gizmo3D.Render();

        var hitinfo = uiFrame.Paper.Column("hitinfo")
            .PositionType(PositionType.SelfDirected)
            .Top(0)
            .Left(0)
            .Margin(UnitValue.StretchOne, 0)
            .BackgroundColor(Components.Style.FrameBackground)
            .Width(UnitValue.Auto)
            .Height(UnitValue.Auto)
            .MinWidth(64)
            .MaxHeight(0)
            .Clip()
            .Border(8)
            .Transition(GuiProp.MaxHeight, 0.1)
            .Rounded(0, 0, 8, 8);
        using (hitinfo.Enter())
        {
            if (world.Raycast(Camera.Ray, 5f, out var hit))
            {
                hitinfo.MaxHeight(300);
                Gizmo3D.DrawBoundingBox(new BoundingBox(hit.Position, 1, 1, 1));

                var identifier = Identifier.Parse(hit.Block.Name);
                using (uiFrame.Paper.Row("debuginfo")
                           .Height(UnitValue.Auto)
                           .Enter())
                {
                    Components.Text(identifier.module, 12f, FontFamily.Monospace)
                        .TextColor(Components.Style.SecondaryTextColor).Width(UnitValue.StretchOne);
                    Components.Text(hit.BlockId.ToString(), 12f, FontFamily.Monospace)
                        .TextColor(Components.Style.SecondaryTextColor);
                }

                Components.Text("Variant: " + Array.IndexOf(hit.Block.Variants, hit.Block.GetVariant(hit.Position.Id)), 12f, FontFamily.Monospace)
                        .TextColor(Components.Style.SecondaryTextColor);
                    


                Components.Text(identifier.name, 18f).Margin(UnitValue.StretchOne,0);
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