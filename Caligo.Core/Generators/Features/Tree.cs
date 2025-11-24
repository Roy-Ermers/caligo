using Caligo.Core.ModuleSystem;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.ModuleSystem;
using Random = Caligo.Core.Utils.Random;

namespace Caligo.Core.Generators.Features;

public class Tree : Feature
{
    private readonly Block LogBlock;
    private readonly Block LeafBlock;
    private readonly short Height;

    private WorldPosition Seed;

    public Tree(Random random, WorldPosition seed) : base(random)
    {
        LogBlock = ModuleRepository.Current.Get<Block>("log");
        LeafBlock = ModuleRepository.Current.Get<Block>("leaves");
        Seed = seed;
        Height = (short)Random.Next(16, 24);
        BoundingBox = new BoundingBox(seed.X - 7, seed.Y, seed.Z - 7, seed.X + 7, seed.Y + Height, seed.Z + 7);
    }

    public override ushort GetBlock(WorldPosition position)
    {
        var y = position.Y - Seed.Y;

        float leafRadius = BoundingBox.Width / 2f - 1f;
        if (position.X == Seed.X && position.Z == Seed.Z && y <= Height - leafRadius / 2)
        {
            return LogBlock.NumericId;
        }

        if (y >= Height - leafRadius * 2 && y <= Height && leafRadius > 0)
        {
            var dx = position.X - Seed.X;
            var dy = position.Y - (Seed.Y + Height - leafRadius);
            var dz = position.Z - Seed.Z;

            var distanceFromCenter = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            if (distanceFromCenter <= leafRadius)
            {
                return LeafBlock.NumericId;
            }
        }

        return 0;
    }
}
