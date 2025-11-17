using WorldGen.Debugging.UI.Modules;
using WorldGen.Graphics.UI.PaperComponents;
using WorldGen.Universe;

namespace WorldGen.Debugging.UI.Modules;

public class ChunkDebugModule : IDebugModule
{
    public bool Enabled { get; set; }
    public string Name => "Chunks";
    public char? Icon => PaperIcon.Apps;

    private readonly World _world;
    private readonly Game _game;

    public ChunkDebugModule(Game game)
    {
        _game = game;
        _world = game.world;
    }

    public void Render()
    {
        if (_world is null)
            return;

        Components.Text("Current Chunks: " + _world.ChunkLoaders.Length);

        if (Components.Button("Unload All Chunks"))
        {
            foreach (var chunkLoader in _world.ChunkLoaders)
            {
                _world.RemoveChunk(chunkLoader.Position);
            }
            _game.renderer.Clear();
        }

        if (Components.Accordion("Chunks"))
        {
            using var scrollContainer = Components.ScrollContainer().Enter();

            foreach (var loader in _world.ChunkLoaders)
            {
                var state = ChunkState.None;
                var (position, ticks) = loader;

                if (_world.TryGetChunk(position, out var chunk))
                    state = chunk.State;

                using (Components.Row())
                {
                    Components.Text($"Position: {position.X,3} {position.Y,3} {position.Z,3}", fontFamily: FontFamily.Monospace);
                    Components.Text($"Ticks: {ticks + 1}");
                    Components.Text($"State: {state}");
                }
            }
        }
    }
}
