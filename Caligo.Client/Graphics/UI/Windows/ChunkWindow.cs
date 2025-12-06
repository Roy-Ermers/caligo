using Caligo.Client.Graphics.UI.ImGuiComponents.Tables;
using Caligo.Core.Universe;
using ImGuiNET;
using World = Caligo.Core.Universe.Worlds.World;

namespace Caligo.Client.Graphics.UI.Windows;

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

        ImGui.TextDisabled("Current Chunks: " + _world.LoadedChunks.Count);

        if (ImGui.Button("Unload All Chunks"))
        {
            foreach (var position in _world.LoadedChunks)
            {
                _world.RemoveChunk(position);
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
                  "State",
                ],
                Border = true,
            };

            foreach (var position in _world.LoadedChunks)
            {
                var state = ChunkState.None;

                if (_world.TryGetChunk(position, out var chunk))
                    state = chunk.State;

                var tableRow = new TableRowComponent(
                $"{position.X,3} {position.Y,3} {position.Z,3}",
                state.ToString());

                table.AddRow(
                            tableRow
                );
            }
        }
    }
}
