using ImGuiNET;
using WorldGen.Graphics.UI.Components;
using WorldGen.Graphics.UI.Components.Tables;
using WorldGen.Universe;

namespace WorldGen.Graphics.UI.Windows;

public class ChunkWindow : Window
{
    public override bool Enabled => true;
    public override string Name => "Chunks";
    World _world = null!;
    Game _game = null!;

    public override void Initialize(Game game)
    {
        base.Initialize(game);

        _game = game;
        _world = game.world;
    }
    public override void Draw(double deltaTime)
    {
        if (_world is null)
            return;

        ImGui.TextDisabled("Current Chunks: " + _world.ChunkLoaders.Length);

        if (ImGui.Button("Unload All Chunks"))
        {
            foreach (var chunkLoader in _world.ChunkLoaders)
            {
                _world.RemoveChunk(chunkLoader.Position);
            }
            _game.renderer.Clear();
        }
        if (ImGui.CollapsingHeader("Chunks"))
        {
            using var table = new TableComponent("Loaded chunks")
            {
                EnableVirtualization = true,
                Headers = [
                  "Position",
                  "Ticks Left",
                  "State",
                ],
                Border = true,
            };

            foreach (var loader in _world.ChunkLoaders)
            {
                var state = ChunkState.None;
                var (position, ticks) = loader;

                if (_world.TryGetChunk(position, out var chunk))
                    state = chunk.State;

                var tableRow = new TableRowComponent(
                $"{position.X,3} {position.Y,3} {position.Z,3}",
                (ticks + 1).ToString(),
                state.ToString());

                table.AddRow(
                            tableRow
                );
            }
        }
    }
}
