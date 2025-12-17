using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.ModuleSystem;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Caligo.Client.Player;

/// <summary>
///     First person character controller for player movement and camera control.
/// </summary>
public class PlayerController : IController
{
    // Player dimensions
    private const float PlayerHeight = 1.8f;
    private const float PlayerRadius = 0.3f;
    private readonly Game _game;
    private bool _isGrounded;
    private float _pitch;

    // Movement properties
    private Vector3 _velocity;

    // Camera rotation
    private float _yaw;
    private bool _cursorLocked;
    private bool _firstMouseMove;
    private Vector2 _lastMousePosition;

    // Smooth stepping visual offset
    private float _stepYOffset;
    private float StepSmoothSpeed { get; } = 16.0f; // How fast the camera catches up (units/sec)

    public PlayerController(Game game)
    {
        _game = game;
        _velocity = Vector3.Zero;

        // Initialize rotation from camera's current state
        _yaw = game.Camera.Yaw;
        _pitch = game.Camera.Pitch;
    }

    // Configuration
    private float MoveSpeed { get; } = 5.0f;
    private float SprintMultiplier { get; } = 2.0f;
    private float JumpForce { get; } = 8.0f;
    private float Gravity { get; } = 20.0f;
    private float TerminalVelocity { get; } = 50.0f;
    private float MouseSensitivity { get; } = 0.002f;
    private float StepHeight { get; } = 2.1f; // max auto-step height (in blocks)

    /// <summary>
    ///     Updates player movement and camera rotation.
    /// </summary>
    public void Update(double deltaTime)
    {
        ProcessMovement();
        ProcessMouseLook();
        var dt = (float)deltaTime;

        // Apply gravity
        if (!_isGrounded)
        {
            _velocity.Y -= Gravity * dt;
            _velocity.Y = Math.Max(_velocity.Y, -TerminalVelocity);
        }

        // Calculate movement from velocity
        var newPosition = _game.Camera.Position + _velocity * dt;

        // Check collisions and update position
        newPosition = ResolveCollisions(newPosition, dt);

        // Smooth the step offset toward zero
        if (MathF.Abs(_stepYOffset) > 0.001f)
        {
            var smoothAmount = StepSmoothSpeed * dt;
            if (_stepYOffset > 0)
                _stepYOffset = MathF.Max(0, _stepYOffset - smoothAmount);
            else
                _stepYOffset = MathF.Min(0, _stepYOffset + smoothAmount);
        }
        else
        {
            _stepYOffset = 0;
        }

        // Apply physics position plus visual offset
        _game.Camera.Position = newPosition + new Vector3(0, _stepYOffset, 0);

        // Check if grounded for next frame (use physics position, not visual)
        var physicsPos = _game.Camera.Position - new Vector3(0, _stepYOffset, 0);
        _isGrounded = CheckGroundedAt(physicsPos);
    }

    /// <summary>
    ///     Handles keyboard input for player movement.
    /// </summary>
    public void ProcessMovement()
    {
        var forward = _game.KeyboardState.IsKeyDown(Keys.W);
        var backward = _game.KeyboardState.IsKeyDown(Keys.S);
        var left = _game.KeyboardState.IsKeyDown(Keys.A);
        var right = _game.KeyboardState.IsKeyDown(Keys.D);
        var jump = _game.KeyboardState.IsKeyDown(Keys.Space);
        var sprint = _game.KeyboardState.IsKeyDown(Keys.LeftShift) || _game.KeyboardState.IsKeyDown(Keys.RightShift);
        var moveDirection = Vector3.Zero;

        // Get camera forward and right vectors (ignoring Y component for horizontal movement)
        var cameraForward = _game.Camera.Forward;
        cameraForward.Y = 0;
        if (cameraForward.LengthSquared > 0.0001f)
            cameraForward = Vector3.Normalize(cameraForward);

        var cameraRight = _game.Camera.Right;
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
    public void ProcessMouseLook()
    {
        // Toggle cursor lock with Escape
        if (_game.KeyboardState.IsKeyPressed(Keys.Escape))
        {
            _cursorLocked = !_cursorLocked;
            _game.CursorState = _cursorLocked ? CursorState.Grabbed : CursorState.Normal;
            _firstMouseMove = true; // Reset to avoid camera jump when re-locking
        }

        // Mouse look (only when cursor is locked)
        var mouse = _game.MouseState;
        var mousePos = new Vector2(mouse.X, mouse.Y);
        if (_firstMouseMove)
        {
            _lastMousePosition = mousePos;
            _firstMouseMove = false;
        }

        var delta = mousePos - _lastMousePosition;
        if (!_cursorLocked)
        {
            return;
        }

        _lastMousePosition = mousePos;

        _yaw += delta.X * MouseSensitivity;
        _pitch -= delta.Y * MouseSensitivity;

        // Clamp pitch to prevent flipping
        _pitch = Math.Clamp(_pitch, -MathF.PI / 2 + 0.01f, MathF.PI / 2 - 0.01f);

        // Update camera rotation
        _game.Camera.Yaw = _yaw;
        _game.Camera.Pitch = _pitch;
    }

    /// <summary>
    ///     Resolves collisions with blocks in the world.
    /// </summary>
    private Vector3 ResolveCollisions(Vector3 targetPosition, float dt)
    {
        // Get physics position (without visual offset)
        var result = _game.Camera.Position - new Vector3(0, _stepYOffset, 0);

        // X axis with auto-step
        var testX = result;
        testX.X = targetPosition.X;
        if (!CheckCollision(testX))
        {
            result.X = targetPosition.X;
        }
        else if (_isGrounded && TryStepUp(ref result, targetPosition.X, axis: 0))
        {
            // Successfully stepped up on X
        }
        else
        {
            _velocity.X = 0;
        }

        // Z axis with auto-step
        var testZ = result;
        testZ.Z = targetPosition.Z;
        if (!CheckCollision(testZ))
        {
            result.Z = targetPosition.Z;
        }
        else if (_isGrounded && TryStepUp(ref result, targetPosition.Z, axis: 2))
        {
            // Successfully stepped up on Z
        }
        else
        {
            _velocity.Z = 0;
        }

        // Y axis
        var testY = result;
        testY.Y = targetPosition.Y;
        if (!CheckCollision(testY))
        {
            result.Y = targetPosition.Y;
        }
        else
        {
            _velocity.Y = 0;
        }

        return result;
    }

    /// <summary>
    ///     Attempts to step up over an obstacle on the given axis.
    /// </summary>
    private bool TryStepUp(ref Vector3 position, float targetAxisValue, int axis)
    {
        if (StepHeight <= 0f) return false;

        var originalY = position.Y;

        // Try stepping up in small increments to find the minimum step height needed
        const float stepIncrement = 0.1f;

        for (var stepUp = stepIncrement; stepUp <= StepHeight; stepUp += stepIncrement)
        {
            // Check if we have headroom at this height
            var raised = position;
            raised.Y += stepUp;
            if (CheckCollision(raised))
                continue; // Head would hit something, try higher or fail

            // Check if we can move horizontally at this raised height
            var raisedMove = raised;
            if (axis == 0) raisedMove.X = targetAxisValue;
            else raisedMove.Z = targetAxisValue;

            if (CheckCollision(raisedMove))
                continue; // Still blocked at this height, try higher

            // Success! Snap down to land on the surface
            var landed = SnapDown(raisedMove, stepUp);

            // Calculate how much we actually stepped up
            var actualStepHeight = landed.Y - originalY;

            // Add to the visual offset (negative so camera appears lower, then smooths up)
            if (actualStepHeight > 0.01f)
            {
                _stepYOffset -= actualStepHeight;
            }

            position = landed;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if there's a collision at the given position.
    /// </summary>
    private bool CheckCollision(Vector3 position)
    {
        const float eps = 0.0001f; // reduce face-sticking and precision issues

        var playerMinX = position.X - PlayerRadius + eps;
        var playerMaxX = position.X + PlayerRadius - eps;
        var playerMinY = position.Y - PlayerHeight + eps;
        var playerMaxY = position.Y - eps;
        var playerMinZ = position.Z - PlayerRadius + eps;
        var playerMaxZ = position.Z + PlayerRadius - eps;

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
            if (!_game.World.TryGetBlock(blockPosition, out var blockId)) continue;

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
        var physicsPos = _game.Camera.Position - new Vector3(0, _stepYOffset, 0);
        return CheckGroundedAt(physicsPos);
    }

    /// <summary>
    ///     Checks if the player is standing on solid ground at a specific position.
    /// </summary>
    private bool CheckGroundedAt(Vector3 position)
    {
        // Check a small distance below the player's feet
        var feetY = position.Y - PlayerHeight - 0.05f;

        // Check multiple points under the player (corners and center)
        float[] offsetsX = [0, -PlayerRadius * 0.5f, PlayerRadius * 0.5f];
        float[] offsetsZ = [0, -PlayerRadius * 0.5f, PlayerRadius * 0.5f];

        foreach (var ox in offsetsX)
        foreach (var oz in offsetsZ)
        {
            var blockPosition = new WorldPosition(
                (int)Math.Floor(position.X + ox),
                (int)Math.Floor(feetY),
                (int)Math.Floor(position.Z + oz)
            );

            if (!_game.World.TryGetBlock(blockPosition, out var blockId)) continue;

            var block = ModuleRepository.Current.GetAll<Block>()[blockId];
            if (block.IsSolid) return true;
        }

        return false;
    }

    /// <summary>
    ///     Moves down up to 'maxDown' while staying collision-free to rest on the surface.
    /// </summary>
    private Vector3 SnapDown(Vector3 position, float maxDown)
    {
        const float step = 0.05f;
        var result = position;
        var remaining = maxDown;

        while (remaining > 0f)
        {
            var dist = MathF.Min(step, remaining);
            var test = result;
            test.Y -= dist;

            if (CheckCollision(test))
                break;

            result = test;
            remaining -= dist;
        }

        return result;
    }
}