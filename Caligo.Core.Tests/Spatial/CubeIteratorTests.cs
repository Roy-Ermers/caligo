using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;

namespace Caligo.Core.Tests.Spatial;

public class CubeIteratorTests
{
    [Test]
    public async Task IteratesAllPositionsWithinCubeBounds()
    {
        var start = new WorldPosition(0, 0, 0);
        var end = new WorldPosition(3, 3, 3);
        var iterator = new CubeIterator(start, end);
    
        var positions = iterator.ToList();
    
        await Assert.That(positions.Count).IsEqualTo(27);
        await Assert.That(positions).Contains(new WorldPosition(0, 0, 0));
        await Assert.That(positions).Contains(new WorldPosition(2, 2, 2));
        await Assert.That(positions).DoesNotContain(new WorldPosition(3, 3, 3));
    }
    
    [Test]
    public async Task IteratesSinglePositionWhenStartAndEndAreAdjacent()
    {
        var start = new WorldPosition(5, 5, 5);
        var end = new WorldPosition(6, 6, 6);
        var iterator = new CubeIterator(start, end);
    
        var positions = iterator.ToList();
    
        await Assert.That(positions.Count).IsEqualTo(1);
        await Assert.That(positions[0]).IsEqualTo(new WorldPosition(5, 5, 5));
    }
    
    [Test]
    public async Task ReturnsEmptyWhenStartEqualsEnd()
    {
        var position = new WorldPosition(10, 10, 10);
        var iterator = new CubeIterator(position, position);
    
        var positions = iterator.ToList();
    
        await Assert.That(positions).IsEmpty();
    }
    
    [Test]
    public async Task ReturnsEmptyWhenEndIsBeforeStart()
    {
        var start = new WorldPosition(5, 5, 5);
        var end = new WorldPosition(3, 3, 3);
        var iterator = new CubeIterator(start, end);
    
        var positions = iterator.ToList();
    
        await Assert.That(positions).IsEmpty();
    }
    
    [Test]
    public async Task IteratesInCorrectOrderXThenYThenZ()
    {
        var start = new WorldPosition(0, 0, 0);
        var end = new WorldPosition(2, 2, 2);
        var iterator = new CubeIterator(start, end);
    
        var positions = iterator.ToList();
    
        await Assert.That(positions[0]).IsEqualTo(new WorldPosition(0, 0, 0));
        await Assert.That(positions[1]).IsEqualTo(new WorldPosition(0, 0, 1));
        await Assert.That(positions[2]).IsEqualTo(new WorldPosition(0, 1, 0));
        await Assert.That(positions[4]).IsEqualTo(new WorldPosition(1, 0, 0));
    }
    
    [Test]
    public async Task HandlesNegativeCoordinates()
    {
        var start = new WorldPosition(-2, -2, -2);
        var end = new WorldPosition(1, 1, 1);
        var iterator = new CubeIterator(start, end);
    
        var positions = iterator.ToList();
    
        await Assert.That(positions.Count).IsEqualTo(27);
        await Assert.That(positions).Contains(new WorldPosition(-2, -2, -2));
        await Assert.That(positions).Contains(new WorldPosition(0, 0, 0));
        await Assert.That(positions).DoesNotContain(new WorldPosition(1, 1, 1));
    }
    
    [Test]
    public async Task IteratesRectangularVolume()
    {
        var start = new WorldPosition(0, 0, 0);
        var end = new WorldPosition(4, 2, 3);
        var iterator = new CubeIterator(start, end);
    
        var positions = iterator.ToList();
    
        await Assert.That(positions.Count).IsEqualTo(24);
    }

    [Test]
    public async Task IteratesThroughAChunk()
    {
        var chunk = new Chunk(ChunkPosition.Zero);
        
        var iterator = new CubeIterator(chunk);
        var positions = iterator.ToList();
        await Assert.That(positions.Count).IsEqualTo(Chunk.Size * Chunk.Size * Chunk.Size);
    }
    
}