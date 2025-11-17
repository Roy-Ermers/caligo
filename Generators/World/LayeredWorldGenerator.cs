using WorldGen.Generators.Transport;
using WorldGen.ModuleSystem;
using WorldGen.Resources.Block;
using WorldGen.Universe;
using WorldGen.Utils;

namespace WorldGen.Generators.World;

public class LayeredWorldGenerator : IWorldGenerator
{
    private readonly int Seed;
    private readonly TransportNetwork Network;
    private readonly Block TerrainBlock;
    private readonly Block NodeBlock;

    public LayeredWorldGenerator(int seed)
    {
        Seed = seed;
        Network = new TransportNetwork(seed);

        TerrainBlock = ModuleRepository.Current.Get<Block>("dirt");
        NodeBlock = ModuleRepository.Current.Get<Block>("node");
    }

    public void GenerateChunk(ref Chunk chunk)
    {
        // var transportNode = Network.GetNode(chunk.Position.ToWorldPosition());

        foreach (var position in new CubeIterator(chunk))
        {
            if (position.Y == 1 && position.X == 0 && position.Z == 0)
                chunk.Set(position.ChunkLocalPosition, NodeBlock);
            // if (transportNode.BoundingBox.Contains(position))
            else if (position.Y <= 0)
            {
                chunk.Set(position.ChunkLocalPosition, TerrainBlock);
            }
        }
    }

    public void Initialize()
    {

    }
}
