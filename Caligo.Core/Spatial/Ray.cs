using System.Numerics;

namespace Caligo.Core.Spatial;

public struct Ray
{
    public readonly Vector3 Origin;
    public readonly Vector3 Direction;
    
    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = Vector3.Normalize(direction);
    }
    
    public Vector3 GetPoint(float distance)
    {
        return Origin + Direction * distance;
    }
    
    public override string ToString()
    {
        return $"Ray(Origin: {Origin}, Direction: {Direction})";
    }
}