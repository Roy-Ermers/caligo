using Caligo.Client.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Caligo.Client.Player;

public class FreeCameraController
{
    private readonly Game _game;

    private Vector2 _lastMousePosition;
    private float _speed = 1f;
    public Camera Camera;

    public FreeCameraController(Game game)
    {
        _game = game;
        Camera = _game.Camera;
    }

    public void Update(double deltaTime)
    {
        var keyboard = _game.KeyboardState;
        var mouse = _game.MouseState;

        _speed = MathHelper.Clamp(_speed + mouse.ScrollDelta.Y, 1, 20);

        var movementSpeed = _speed * (float)deltaTime;

        if (keyboard.IsKeyDown(Keys.LeftShift)) movementSpeed *= 5f;

        if (keyboard.IsKeyDown(Keys.W)) Camera.Position += Camera.Forward * movementSpeed;

        if (keyboard.IsKeyDown(Keys.S)) Camera.Position -= Camera.Forward * movementSpeed;

        if (keyboard.IsKeyDown(Keys.A)) Camera.Position -= Camera.Right * movementSpeed;

        if (keyboard.IsKeyDown(Keys.D)) Camera.Position += Camera.Right * movementSpeed;

        if (keyboard.IsKeyDown(Keys.Space)) Camera.Position += Camera.Up * movementSpeed;

        if (keyboard.IsKeyDown(Keys.LeftControl)) Camera.Position -= Camera.Up * movementSpeed;

        // lock cursor
        _game.CursorState = mouse.IsButtonDown(MouseButton.Right) ? CursorState.Grabbed : CursorState.Normal;
        if (mouse.IsButtonDown(MouseButton.Right))
        {
            var delta = new Vector2(
                mouse.X - _lastMousePosition.X,
                mouse.Y - _lastMousePosition.Y
            );

            Camera.Yaw += MathHelper.DegreesToRadians(delta.X * 0.2f);
            Camera.Pitch = MathHelper.Clamp(Camera.Pitch - MathHelper.DegreesToRadians(delta.Y * 0.2f),
                -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);
        }

        _lastMousePosition = new Vector2(mouse.X, mouse.Y);
    }
}