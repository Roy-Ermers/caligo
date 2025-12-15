using System.Numerics;

namespace Caligo.Core.Utils;

public enum Direction
{
    Down,
    Up,
    North,
    South,
    West,
    East
}

public static class DirectionExtensions
{
    public static Direction Opposite(this Direction direction)
    {
        return direction switch
        {
            Direction.Down => Direction.Up,
            Direction.Up => Direction.Down,
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.West => Direction.East,
            Direction.East => Direction.West,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public static Vector3 ToVector3(this Direction direction)
    {
        return direction switch
        {
            Direction.Down => -Vector3.UnitY,
            Direction.Up => Vector3.UnitY,
            Direction.North => -Vector3.UnitZ,
            Direction.South => Vector3.UnitZ,
            Direction.West => -Vector3.UnitX,
            Direction.East => Vector3.UnitX,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}