using Caligo.Client.Graphics;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe.Worlds;
using Caligo.ModuleSystem;
using OpenTK.Mathematics;

namespace Caligo.Client.Player;

/// <summary>
///     First person character controller for player movement and camera control.
/// </summary>
public class PlayerController
{
    // Player dimensions
    private const float PlayerHeight = 1.8f;
    private const float PlayerRadius = 0.3f;
    private readonly Camera _camera;
    private readonly World _world;
    private bool _isGrounded;
    private float _pitch;

    // Movement properties
    private Vector3 _velocity;

    // Camera rotation
    private float _yaw;

    public PlayerController(Camera camera, World world)
    {
        _world = world;
        _camera = camera;
        _velocity = Vector3.Zero;

        // Initialize rotation from camera's current state
        _yaw = camera.Yaw;
        _pitch = camera.Pitch;
    }

    // Configuration
    private float MoveSpeed { get; } = 5.0f;
    private float SprintMultiplier { get; } = 2.0f;
    private float JumpForce { get; } = 8.0f;
    private float Gravity { get; } = 20.0f;
    private float TerminalVelocity { get; } = 50.0f;
    private float MouseSensitivity { get; } = 0.002f;

    /// <summary>
    ///     Updates player movement and camera rotation.
    /// </summary>
    public void Update(double deltaTime)
    {
        var dt = (float)deltaTime;

        // Apply gravity
        if (!_isGrounded)
        {
            _velocity.Y -= Gravity * dt;
            _velocity.Y = Math.Max(_velocity.Y, -TerminalVelocity);
        }

        // Calculate movement from velocity
        var newPosition = _camera.Position + _velocity * dt;

        // Check collisions and update position
        newPosition = ResolveCollisions(newPosition);

        _camera.Position = newPosition;

        // Check if grounded for next frame
        _isGrounded = CheckGrounded();
    }

    /// <summary>
    ///     Handles keyboard input for player movement.
    /// </summary>
    public void ProcessMovement(bool forward, bool backward, bool left, bool right, bool jump, bool sprint)
    {
        var moveDirection = Vector3.Zero;

        // Get camera forward and right vectors (ignoring Y component for horizontal movement)
        var cameraForward = _camera.Forward;
        cameraForward.Y = 0;
        if (cameraForward.LengthSquared > 0.0001f)
            cameraForward = Vector3.Normalize(cameraForward);

        var cameraRight = _camera.Right;
        cameraRight.Y = 0;
        if (cameraRight.LengthSquared > 0.0001f)
            cameraRight = Vector3.Normalize(cameraRight);

        // Build movement direction
        if (forward) moveDirection += cameraForward;
        if (backward) moveDirection -= cameraForward;
        if (right) moveDirection += cameraRight;
        if (left) moveDirection -= cameraRight;

        // Normalize diagonal movement
        if (moveDirection.LengthSquared > 0.0001f) moveDirection = Vector3.Normalize(moveDirection);

        // Apply speed
        var speed = MoveSpeed * (sprint ? SprintMultiplier : 1.0f);
        _velocity.X = moveDirection.X * speed;
        _velocity.Z = moveDirection.Z * speed;

        // Jump
        if (!jump || !_isGrounded) return;

        _velocity.Y = JumpForce;
        _isGrounded = false;
    }

    /// <summary>
    ///     Handles mouse input for camera rotation.
    /// </summary>
    public void ProcessMouseLook(float deltaX, float deltaY)
    {
        _yaw += deltaX * MouseSensitivity;
        _pitch -= deltaY * MouseSensitivity;

        // Clamp pitch to prevent flipping
        _pitch = Math.Clamp(_pitch, -MathF.PI / 2 + 0.01f, MathF.PI / 2 - 0.01f);

        // Update camera rotation
        _camera.Yaw = _yaw;
        _camera.Pitch = _pitch;
    }

    /// <summary>
    ///     Resolves collisions with blocks in the world.
    /// </summary>
    private Vector3 ResolveCollisions(Vector3 targetPosition)
    {
        var currentPos = _camera.Position;
        var result = targetPosition;

        // Resolve X axis
        result.X = ResolveAxis(currentPos, result, 0);

        // Resolve Z axis
        result.Z = ResolveAxis(currentPos, result, 2);

        // Resolve Y axis
        result.Y = ResolveAxis(currentPos, result, 1);

        return result;
    }

    /// <summary>
    ///     Resolves collision along a single axis.
    /// </summary>
    private float ResolveAxis(Vector3 currentPos, Vector3 targetPos, int axis)
    {
        // Create test position with only this axis changed
        var testPos = currentPos;
        switch (axis)
        {
            case 0:
                testPos.X = targetPos.X;
                break;
            case 1:
                testPos.Y = targetPos.Y;
                break;
            default:
                testPos.Z = targetPos.Z;
                break;
        }

        if (CheckCollision(testPos))
            switch (axis)
            {
                // Collision detected, keep current position on this axis
                case 0:
                    _velocity.X = 0;
                    return currentPos.X;
                case 1:
                    _velocity.Y = 0;
                    return currentPos.Y;
                default:
                    _velocity.Z = 0;
                    return currentPos.Z;
            }

        return axis switch
        {
            // No collision, allow movement
            0 => targetPos.X,
            1 => targetPos.Y,
            _ => targetPos.Z
        };
    }

    /// <summary>
    ///     Checks if there's a collision at the given position.
    /// </summary>
    private bool CheckCollision(Vector3 position)
    {
        var playerMinX = position.X - PlayerRadius;
        var playerMaxX = position.X + PlayerRadius;
        var playerMinY = position.Y - PlayerHeight;
        var playerMaxY = position.Y;
        var playerMinZ = position.Z - PlayerRadius;
        var playerMaxZ = position.Z + PlayerRadius;

        var minX = (int)Math.Floor(playerMinX);
        var maxX = (int)Math.Floor(playerMaxX);
        var minY = (int)Math.Floor(playerMinY);
        var maxY = (int)Math.Floor(playerMaxY);
        var minZ = (int)Math.Floor(playerMinZ);
        var maxZ = (int)Math.Floor(playerMaxZ);

        for (var x = minX; x <= maxX; x++)
        for (var y = minY; y <= maxY; y++)
        for (var z = minZ; z <= maxZ; z++)
        {
            var blockPosition = new WorldPosition(x, y, z);
            if (!_world.TryGetBlock(blockPosition, out var blockId)) continue;

            var block = ModuleRepository.Current.GetAll<Block>()[blockId];
            if (!block.IsSolid) continue;

            // Block bounds
            float blockMinX = x;
            float blockMaxX = x + 1;
            float blockMinY = y;
            float blockMaxY = y + 1;
            float blockMinZ = z;
            float blockMaxZ = z + 1;

            // Check AABB overlap
            if (playerMaxX > blockMinX && playerMinX < blockMaxX &&
                playerMaxY > blockMinY && playerMinY < blockMaxY &&
                playerMaxZ > blockMinZ && playerMinZ < blockMaxZ)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if the player is standing on solid ground.
    /// </summary>
    private bool CheckGrounded()
    {
        // Check a small distance below the player's feet
        var feetY = _camera.Position.Y - PlayerHeight - 0.05f;

        // Check multiple points under the player (corners and center)
        float[] offsetsX = [0, -PlayerRadius * 0.5f, PlayerRadius * 0.5f];
        float[] offsetsZ = [0, -PlayerRadius * 0.5f, PlayerRadius * 0.5f];

        foreach (var ox in offsetsX)
        foreach (var oz in offsetsZ)
        {
            var blockPosition = new WorldPosition(
                (int)Math.Floor(_camera.Position.X + ox),
                (int)Math.Floor(feetY),
                (int)Math.Floor(_camera.Position.Z + oz)
            );

            if (!_world.TryGetBlock(blockPosition, out var blockId)) continue;

            var block = ModuleRepository.Current.GetAll<Block>()[blockId];
            if (block.IsSolid) return true;
        }

        return false;
    }
}