using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;

namespace WorldGen.Graphics;

public class Camera
{
    public Vector3 Position = new(0, 0, 0);
    private float _speed = 1f;
    public float Pitch = 0f;
    public float Yaw = -MathHelper.PiOver2;

    private Vector3 _forward = -Vector3.UnitZ;

    public Vector3 Forward => _forward;
    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
    public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Forward));

    public Matrix4 ViewMatrix { get; private set; }
    public Matrix4 ProjectionMatrix { get; private set; }

    private Vector2 _lastMousePosition;

    private readonly GameWindow _game;

    public Camera(GameWindow game)
    {
        _game = game;
        UpdateMatrices();
    }

    public void UpdateMatrices()
    {
        ViewMatrix = Matrix4.LookAt(Position, Position + Forward, Vector3.UnitY);

        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver2, _game.Size.X / (float)_game.Size.Y, .1f, 1000f);
    }

    public void Update(double deltaTime)
    {
        var keyboard = _game.KeyboardState;
        var mouse = _game.MouseState;

        _speed = MathHelper.Clamp(_speed + mouse.ScrollDelta.Y, 1, 20);

        var movementSpeed = _speed * (float)deltaTime;

        if (keyboard.IsKeyDown(Keys.LeftShift))
        {
            movementSpeed *= 5f;
        }

        if (keyboard.IsKeyDown(Keys.W))
        {
            Position += Forward * movementSpeed;
        }

        if (keyboard.IsKeyDown(Keys.S))
        {
            Position -= Forward * movementSpeed;
        }

        if (keyboard.IsKeyDown(Keys.A))
        {
            Position -= Right * movementSpeed;
        }

        if (keyboard.IsKeyDown(Keys.D))
        {
            Position += Right * movementSpeed;
        }

        if (keyboard.IsKeyDown(Keys.Space))
        {
            Position += Up * movementSpeed;
        }

        if (keyboard.IsKeyDown(Keys.LeftControl))
        {
            Position -= Up * movementSpeed;
        }

        // lock cursor
        _game.CursorState = mouse.IsButtonDown(MouseButton.Right) ? CursorState.Grabbed : CursorState.Normal;
        if (mouse.IsButtonDown(MouseButton.Right))
        {
            var delta = new Vector2(
                mouse.X - _lastMousePosition.X,
                mouse.Y - _lastMousePosition.Y
             );

            Yaw += MathHelper.DegreesToRadians(delta.X * 0.2f);
            Pitch = MathHelper.Clamp(Pitch - MathHelper.DegreesToRadians(delta.Y * 0.2f), -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);
        }

        _lastMousePosition = new Vector2(mouse.X, mouse.Y);

        _forward.X = MathF.Cos(Pitch) * MathF.Cos(Yaw);
        _forward.Y = MathF.Sin(Pitch);
        _forward.Z = MathF.Cos(Pitch) * MathF.Sin(Yaw);

        _forward = Vector3.Normalize(_forward);

        UpdateMatrices();
    }
    /// <summary>
    /// Projects a world-space position to screen-space coordinates.
    /// Returns a Vector2 where (0,0) is top-left and (windowWidth, windowHeight) is bottom-right. Or null if the position is outside of the camera.
    /// </summary>
    public Vector2? WorldToScreen(Vector3 worldPosition)
    {
        // Transform to clip space
        var clipSpace = new Vector4(worldPosition, 1.0f) * ViewMatrix * ProjectionMatrix;

        // If behind the camera or at infinity, return null
        if (clipSpace.W <= 0)
            return null;

        var ndc = new Vector3(clipSpace.X, clipSpace.Y, clipSpace.Z) / clipSpace.W;

        // If outside the normalized device coordinates, return null
        if (ndc.X < -1f || ndc.X > 1f || ndc.Y < -1f || ndc.Y > 1f || ndc.Z < 0f || ndc.Z > 1f)
            return null;

        // Convert NDC (-1..1) to window coordinates (0..width, 0..height)
        float x = (ndc.X + 1f) * 0.5f * _game.Size.X;
        float y = (1f - ndc.Y) * 0.5f * _game.Size.Y; // Y is inverted

        return new Vector2(x, y);
    }
}
