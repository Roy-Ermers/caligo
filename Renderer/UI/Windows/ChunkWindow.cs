using ImGuiNET;
using WorldGen.Universe;

namespace WorldGen.Renderer.UI.Windows;

public class ChunkWindow : Window
{
    public override bool Enabled => true;
    public override string Name => "Chunks";
    World _world = null!;
    public override void Initialize(Game game)
    {
        base.Initialize(game);

        _world = game.world;
    }
    public override void Draw(double deltaTime)
    {
        if (_world is null)
            return;

        ImGui.TextDisabled("Current Chunks: " + _world.chunkLoaders.Count);

        if (ImGui.Button("Unload All Chunks"))
        {
            foreach (var chunkLoader in _world.chunkLoaders.Keys.ToList())
            {
                _world.UnloadChunk(chunkLoader);
                _world.ChunkRenderer.Clear();
            }
        }
        if (ImGui.CollapsingHeader("Chunks") && ImGui.BeginListBox("Chunks"))
        {
            foreach (var chunkLoader in _world.chunkLoaders)
            {
                ImGui.Text($"Chunk: {chunkLoader.Key} - Ticks: {chunkLoader.Value.Ticks}");
            }
            ImGui.EndListBox();
            ImGui.EndChild();
        }
    }
}
