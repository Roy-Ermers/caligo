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
    private readonly short Radius;

    private WorldPosition Seed;

    public Tree(Random random, WorldPosition seed) : base(random)
    {
        LogBlock = ModuleRepository.Current.Get<Block>("log");
        LeafBlock = ModuleRepository.Current.Get<Block>("leaves");
        Seed = seed;
        Height = (short)Random.Next(8, 32);
        Radius = (short)Math.Max(4, Height / 4);
        BoundingBox = new BoundingBox(seed.X - Radius, seed.Y, seed.Z - Radius, seed.X + Radius, seed.Y + Height, seed.Z + Radius);
    }

    public override ushort GetBlock(WorldPosition position)
    {
        float y = position.Y - Seed.Y;

        var leafRadius = BoundingBox.Width / 2f - 1f;
        if (position.X == Seed.X && position.Z == Seed.Z && y <= Height - leafRadius)
        {
            return LogBlock.NumericId;
        }

        var distanceFromCenter = MathF.Sqrt(MathF.Pow(position.X - Seed.X, 2) + MathF.Pow(position.Z - Seed.Z, 2));
        var heightFactor = 1f - y / Height;
        var currentLeafRadius = leafRadius * heightFactor;
        if (distanceFromCenter <= currentLeafRadius && y >= Height / 4f)
        {
            return LeafBlock.NumericId;
        }

        return 0;
    }
}
