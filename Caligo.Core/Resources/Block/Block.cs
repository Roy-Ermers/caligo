using Caligo.Core.Resources.Block.Models;
using Caligo.Core.Utils;
using Caligo.ModuleSystem;
using Random = Caligo.Core.Utils.Random;

namespace Caligo.Core.Resources.Block;

public record struct BlockVariant
{
    public string ModelName;
    public BlockModel Model;
    public int Weight;
    public Dictionary<string, string> Textures;
}

public record class Block
{
    public static readonly Block Air = new()
    {
        Name = Identifier.Resolve("air"),
    };
    
    public ushort NumericId = 0;
    public bool IsSolid;
    public required string Name;

    public BlockVariant[] Variants
    {
        get;
        set
        {
            field = value;
            RecalculateWeights();
        }
    } = [];

    private int _variantTotal = 0;
    private int[] _cumulativeWeights = [];

    public void RecalculateWeights()
    {
        _variantTotal = Variants.Sum(v => v.Weight);

        // Build cumulative weights array for efficient binary search
        _cumulativeWeights = new int[Variants.Length];
        var cumulative = 0;
        for (var i = 0; i < Variants.Length; i++)
        {
            cumulative += Variants[i].Weight;
            _cumulativeWeights[i] = cumulative;
        }

    }

    public BlockVariant? GetRandomVariant(Random? random)
    {
        random ??= new Random();
        var seed = random.Next(0, _variantTotal);
        return GetVariant(seed);
    }

    public BlockVariant? GetVariant(int seed)
    {
        if (Variants.Length == 0)
            return null;

        if (Variants.Length == 1)
            return Variants[0];
        
        var target = seed % _variantTotal;
        
        // Binary search through cumulative weights
        var low = 0;
        var high = Variants.Length - 1;

        while (low < high)
        {
            var mid = (low + high) / 2;

            if (target < _cumulativeWeights[mid])
                high = mid;
            else
                low = mid + 1;
        }

        return Variants[low];
    }
}
