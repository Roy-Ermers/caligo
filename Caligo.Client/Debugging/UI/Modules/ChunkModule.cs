using Caligo.Client.Graphics.UI.PaperComponents;
using Caligo.Core.Universe;

namespace Caligo.Client.Debugging.UI.Modules;

public class ChunkDebugModule : IDebugModule
{
    public bool Enabled { get; set; }
    public string Name => "Chunks";
    public char? Icon => PaperIcon.Apps;

    private readonly Game _game;

    public ChunkDebugModule(Game game)
    {
        _game = game;
    }

    public void Render()
    {
        if (_game.world is null)
            return;

        Components.Text("Current Chunks: " + _game.world.ChunkLoaders.Count());

        if (Components.Button("Unload All Chunks"))
        {
            foreach (var chunkLoader in _game.world.ChunkLoaders)
            {
                _game.world.RemoveChunk(chunkLoader.Position);
            }
            _game.renderer.Clear();
        }

        if (Components.Accordion("Chunks"))
        {
            using var scrollContainer = Components.ScrollContainer(Prowl.PaperUI.Scroll.ScrollXY).Enter();

            var id = 0;
            foreach (var loader in _game.world.ChunkLoaders)
            {
                var state = ChunkState.None;
                var (position, ticks) = loader;

                if (_game.world.TryGetChunk(position, out var chunk))
                    state = chunk.State;

                using (Components.Row(8, id++))
                {
                    Components.Text($"Position: {position.X,3} {position.Y,3} {position.Z,3}", fontFamily: FontFamily.Monospace);
                    Components.Text($"State: {state}");
                }
            }
        }
    }
}
