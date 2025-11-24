using System.Numerics;
using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Tests.Spatial.PositionTypes;

public class WorldPositionTests
{
    [Test]
    public async Task WorldPosition_CreatesWithCorrectCoordinates()
    {
        var pos = new WorldPosition(1, 2, 3);
        await Assert.That(pos.X).IsEqualTo(1);
        await Assert.That(pos.Y).IsEqualTo(2);
        await Assert.That(pos.Z).IsEqualTo(3);
    }

    [Test]
    public async Task WorldPosition_Zero_IsOrigin()
    {
        await Assert.That(WorldPosition.Zero).IsEqualTo(new WorldPosition(0, 0, 0));
    }

    [Test]
    public async Task WorldPosition_Id_IsConsistent()
    {
        var pos1 = new WorldPosition(5, 6, 7);
        var pos2 = new WorldPosition(5, 6, 7);
        await Assert.That(pos1.Id).IsEqualTo(pos2.Id);
    }

    [Test]
    public async Task WorldPosition_AddsTwoPositionsCorrectly()
    {
        var a = new WorldPosition(1, 2, 3);
        var b = new WorldPosition(4, 5, 6);
        var result = a + b;

        await Assert.That(result).IsEqualTo(new WorldPosition(5, 7, 9));
    }

    [Test]
    public async Task WorldPosition_SubtractsTwoPositionsCorrectly()
    {
        var a = new WorldPosition(10, 10, 10);
        var b = new WorldPosition(1, 2, 3);
        var result = a - b;
        await Assert.That(result).IsEqualTo(new WorldPosition(9, 8, 7));
    }

    [Test]
    public async Task WorldPosition_AddsIntCorrectly()
    {
        var pos = new WorldPosition(1, 2, 3);
        var result = pos + 5;
        await Assert.That(result).IsEqualTo(new WorldPosition(6, 7, 8));
    }

    [Test]
    public async Task WorldPosition_SubtractsIntCorrectly()
    {
        var pos = new WorldPosition(10, 10, 10);
        var result = pos - 3;
        await Assert.That(result).IsEqualTo(new WorldPosition(7, 7, 7));
    }

    [Test]
    public async Task WorldPosition_DividesByIntCorrectly()
    {
        var pos = new WorldPosition(10, 20, 30);
        var result = pos / 10;
        await Assert.That(result).IsEqualTo(new WorldPosition(1, 2, 3));
    }

    [Test]
    public async Task WorldPosition_ImplicitTupleConversion_Works()
    {
        WorldPosition pos = (7, 8, 9);
        await Assert.That(pos).IsEqualTo(new WorldPosition(7, 8, 9));
    }

    [Test]
    public async Task WorldPosition_ImplicitVector3Conversion_Works()
    {
        var pos = new WorldPosition(1, 2, 3);
        Vector3 vec = pos;
        await Assert.That(vec).IsEqualTo(new Vector3(1, 2, 3));
    }

    [Test]
    public async Task WorldPosition_AddsVector3Correctly()
    {
        var pos = new WorldPosition(1, 2, 3);
        var vec = new Vector3(4, 5, 6);
        var result = pos + vec;
        await Assert.That(result).IsEqualTo(new WorldPosition(5, 7, 9));
    }

    [Test]
    public async Task WorldPosition_ToString_ReturnsExpectedFormat()
    {
        var pos = new WorldPosition(1, 2, 3);
        await Assert.That(pos.ToString()).IsEqualTo("(W;1, 2, 3)");
    }

    [Test]
    public async Task WorldPosition_Deconstruct_ReturnsCorrectValues()
    {
        var pos = new WorldPosition(4, 5, 6);
        pos.Deconstruct(out var x, out var y, out var z);
        
        await Assert.That(x).IsEqualTo(pos.X);
        await Assert.That(y).IsEqualTo(pos.Y);
        await Assert.That(z).IsEqualTo(pos.Z);
    }
    
    [Test]
    public async Task WorldPosition_Is_Correctly_Converted_To_LocalChunkPosition() 
    {
        var worldPos = new WorldPosition(18, 34, 50);
        var localChunkPos = worldPos.ChunkLocalPosition; // Should be (0,0,0)

        await Assert.That(localChunkPos.X).IsEqualTo(2); // 18 % 16
        await Assert.That(localChunkPos.Y).IsEqualTo(2); // 34 % 16
        await Assert.That(localChunkPos.Z).IsEqualTo(2); // 50 % 16
    }  
    
    [Test]
    public async Task Negative_WorldPosition_Is_Correctly_Converted_To_LocalChunkPosition() 
    {
        var worldPos = new WorldPosition(-18, -34, -50);
        var localChunkPos = worldPos.ChunkLocalPosition; // Should be (0,0,0)

        await Assert.That(localChunkPos.X).IsEqualTo(14); // 18 % 16
        await Assert.That(localChunkPos.Y).IsEqualTo(14); // 34 % 16
        await Assert.That(localChunkPos.Z).IsEqualTo(14); // 50 % 16
    }
}