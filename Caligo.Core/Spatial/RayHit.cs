using System.Numerics;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.ModuleSystem;

namespace Caligo.Core.Spatial;

public record struct RaycastHit
{
    public ushort BlockId;
    public Block Block => ModuleRepository.Current.GetAll<Block>()[BlockId];
    public WorldPosition Position;
    public Vector3 HitPoint;
    public Vector3 Normal;
    public float Distance;
}