using Caligo.Client.Graphics.UI.PaperComponents;
using Caligo.Core.Universe;
using Prowl.PaperUI;

namespace Caligo.Client.Debugging.UI.Modules;

public class ChunkDebugModule : IDebugModule
{
    private readonly Game _game;

    public ChunkDebugModule(Game game)
    {
        _game = game;
    }

    public bool Enabled { get; set; }
    public string Name => "Chunks";
    public char? Icon => PaperIcon.Apps;

    public void Render()
    {
        if (_game.world is null)
            return;

        Components.Text("Current Chunks: " + _game.world.LoadedChunks.Count);

        if (Components.Button("Unload All Chunks"))
        {
            foreach (var chunkLoader in _game.world.LoadedChunks) _game.world.RemoveChunk(chunkLoader);
            _game.renderer.Clear();
        }

        if (Components.Accordion("Chunks"))
        {
            using var scrollContainer = Components.ScrollContainer(Scroll.ScrollXY).Enter();

            var id = 0;
            foreach (var position in _game.world.LoadedChunks)
            {
                var state = ChunkState.None;

                if (_game.world.TryGetChunk(position, out var chunk))
                    state = chunk.State;

                using (Components.Row(8, id++))
                {
                    Components.Text($"Position: {position.X,3} {position.Y,3} {position.Z,3}",
                        fontFamily: FontFamily.Monospace);
                    Components.Text($"State: {state}");
                }
            }
        }
    }
}