using System.Numerics;

namespace WorldGen.Resources.Block;

public enum Direction
{
    Down,
    Up,
    North,
    South,
    West,
    East
}


static class DirectionExtensions
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
            Direction.Down => new Vector3(0, -1, 0),
            Direction.Up => new Vector3(0, 1, 0),
            Direction.North => new Vector3(0, 0, -1),
            Direction.South => new Vector3(0, 0, 1),
            Direction.West => new Vector3(-1, 0, 0),
            Direction.East => new Vector3(1, 0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}