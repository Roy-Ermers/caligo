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
using Prowl.PaperUI;
using WorldGen.Graphics.UI.PaperComponents;
using Prowl.PaperUI.LayoutEngine;
using WorldGen.FileSystem;

namespace WorldGen;

public class Game : GameWindow
{
    public readonly Camera Camera;
    public readonly ModuleRepository ModuleRepository;
    // private UiRenderer _uiRenderer;
    private readonly PaperRenderer uiRenderer;

    private readonly RenderShader _shader;

    private readonly RenderDoc? renderdoc;

    public MainThread? MainThread { get; } = new();
    public World world = null!;
    public WorldBuilder builder = null!;
    public WorldRenderer renderer = null!;

    public List<string> SupportedExtensions { get; private set; } = [];

    public Atlas atlas;

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

        atlas = ModuleRepository.Get<Atlas>("block_atlas");
        Camera = new Camera(this);

        MainThread = new MainThread();

        uiRenderer = new(this);
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



        Console.WriteLine("Supported Extensions:");
        int count = GL.GetInteger(GetPName.NumExtensions);
        for (int i = 0; i < count; i++)
        {
            string extension = GL.GetString(StringNameIndexed.Extensions, i);
            SupportedExtensions.Add(extension);
            Console.WriteLine("  " + extension);
        }



        var atlas = ModuleRepository.Get<Atlas>("block_atlas");
        var blockStorage = ModuleRepository.GetAll<Block>();

        world = new World();
        builder = new(world, new LayeredWorldGenerator(Random.Shared.Next()));
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
        using (var uiFrame = uiRenderer.Start(args.Time))
        {
            using var sidebar = uiFrame.Paper.Column("Sidebar").PositionType(PositionType.SelfDirected)
                      .Left(UnitValue.StretchOne)
                      .Top(0)
                      .Bottom(0)
                      .Height(uiFrame.Paper.ScreenRect.Size.y)
                      .Width(400)
                      .ColBetween(16)
                      .ChildRight(16)
                      .ChildTop(16)
                      .ChildBottom(16)
                      // .TranslateX(385)
                      // .Transition(GuiProp.TranslateX, 0.500, Easing.QuartOut)
                      // .Hovered.TranslateX(0).End()
                      .Enter();

            using (Components.Frame("Tabs", renderHeader: false))
            {
                using (Components.Tabs("Tabs", true))
                {
                    if (Components.Tab(PaperIcon.AvgPace + " Stats"))
                    {
                        Components.Text($"FPS: {(int)(1 / args.Time)}");
                        if (Components.Accordion("Garbage collection"))
                        {
                            Components.Text("Total Memory: " + FileSystemUtils.FormatByteSize(GC.GetTotalMemory(false)));
                            Components.Text("Max Memory: " + FileSystemUtils.FormatByteSize(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes));
                            Components.Text("0: " + GC.CollectionCount(0));
                            Components.Text("1: " + GC.CollectionCount(1));
                            Components.Text("2: " + GC.CollectionCount(2));
                        }
                    }
                    if (Components.Tab(PaperIcon.CameraAlt + " Camera"))
                    {

                        var blockPosition = new WorldPosition((int)Camera.Position.X, (int)Camera.Position.Y, (int)Camera.Position.Z);

                        Components.Text("Position: " + Camera.Position);

                        var sector = blockPosition / 64;
                        Components.Text("Sector: " + sector);

                        Components.Text("Chunk: " + blockPosition.ChunkPosition);
                    }

                    if (Components.Tab(PaperIcon.Sdk + " OpenGL"))
                    {
                        var vendor = GL.GetString(StringName.Vendor);
                        if (vendor.Contains("NVIDIA"))
                        {
                            GL.NV.GetInteger((All)0x9048, out long total);
                            GL.NV.GetInteger((All)0x9049, out long current);

                            Components.Text("GPU Memory: " + FileSystemUtils.FormatByteSize(current) + '/' + FileSystemUtils.FormatByteSize(total));
                        }
                        Components.Text("Version: " + GL.GetString(StringName.Version));
                        Components.Text($"Vendor: {vendor}");
                        Components.Text("Renderer: " + GL.GetString(StringName.Renderer));
                        Components.Text("GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));
                        if (Components.Accordion("Extensions"))
                        {
                            var search = "";
                            Components.Textbox(ref search, placeholder: "search", icon: PaperIcon.Search);
                            using var scrollContainer = Components.ScrollContainer().Enter();
                            foreach (var extension in SupportedExtensions)
                            {
                                if (!extension.Contains(search))
                                    continue;
                                Components.Text(extension, fontFamily: FontFamily.Monospace);
                            }
                        }
                    }
                }



                // using var frame = _uiRenderer.StartFrame(args.Time);


                // var oldPos = ImGui.GetCursorScreenPos();

                // var center = Camera.WorldToScreen(Vector3.Zero);

                // if (center is not null)
                // {
                //     ImGui.SetCursorScreenPos(new System.Numerics.Vector2(center.Value.X, center.Value.Y));
                //     ImGui.TextColored(System.Numerics.Vector4.One, "o");
                // }

                // var unitX = Camera.WorldToScreen(Vector3.UnitX);
                // if (unitX is not null)
                // {
                //     ImGui.SetCursorScreenPos(new System.Numerics.Vector2(unitX.Value.X, unitX.Value.Y));
                //     ImGui.TextColored(System.Numerics.Vector4.UnitX + System.Numerics.Vector4.UnitW, "X");
                // }
                // var unitY = Camera.WorldToScreen(Vector3.UnitY);
                // if (unitY is not null)
                // {
                //     ImGui.SetCursorScreenPos(new System.Numerics.Vector2(unitY.Value.X, unitY.Value.Y));
                //     ImGui.TextColored(System.Numerics.Vector4.UnitY + System.Numerics.Vector4.UnitW, "Y");
                // }

                // var unitZ = Camera.WorldToScreen(Vector3.UnitZ);
                // if (unitZ is not null)
                // {
                //     ImGui.SetCursorScreenPos(new System.Numerics.Vector2(unitZ.Value.X, unitZ.Value.Y));
                //     ImGui.TextColored(System.Numerics.Vector4.UnitZ + System.Numerics.Vector4.UnitW, "Z");
                // }

                // var blockCenter = Camera.WorldToScreen(Vector3.One * 0.5f);
                // if (blockCenter is not null)
                // {
                //     ImGui.SetCursorScreenPos(new System.Numerics.Vector2(blockCenter.Value.X, blockCenter.Value.Y));
                //     ImGui.TextColored(System.Numerics.Vector4.One, "B");
                // }

                // ImGui.SetCursorScreenPos(oldPos);

                // _uiRenderer.Draw(args.Time);
            }
        }
    }


    protected override void OnResize(ResizeEventArgs e)
    {
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        // _uiRenderer.WindowResized(Size.X, Size.Y);
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
