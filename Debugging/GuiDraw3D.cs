using System.Collections.Concurrent;
using System.Drawing;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using Prowl.PaperUI;
using WorldGen.Spatial;

namespace WorldGen.Debugging;

readonly record struct LineSegment
(
    Vector3 Start,
    Vector3 End,
    Color Color
);

readonly record struct LineVertex
(
    Vector3 Position,
    Vector3 Color
);

/// <summary>
/// 3D debug rendering system for drawing lines and bounding boxes in world space.
/// Uses modern OpenGL 4 core profile with shaders and vertex buffers.
///
/// Usage example:
/// // Draw a red line from origin to (10, 10, 10)
/// Gizmo3D.DrawLine(Vector3.Zero, new Vector3(10, 10, 10), Color.Red);
///
/// // Draw a green bounding box
/// var bbox = new BoundingBox(new Vector3(-5, -5, -5), new Vector3(5, 5, 5));
/// Gizmo3D.DrawBoundingBox(bbox, Color.Green);
///
/// // Must call Initialize() once during setup and Render() each frame
/// </summary>
public static class Gizmo3D
{
    const float RAD_TO_DEG = 57.29577951308232f;
    const float DEG_TO_RAD = 0.017453292519943295f;
    static Paper gui = null!;
    static Game game = null!;

    private static readonly Lock _lock = new();
    private static readonly Stack<LineSegment> drawCalls = new();

    // OpenGL resources
    private static int _vao;
    private static int _vbo;
    private static int _shaderProgram;
    private static bool _initialized = false;

    // Shader source code
    private const string VertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;

uniform mat4 uViewProjection;

out vec3 vertexColor;

void main()
{
    gl_Position = uViewProjection * vec4(aPosition, 1.0);
    vertexColor = aColor;
}";

    private const string FragmentShaderSource = @"
#version 330 core
in vec3 vertexColor;
out vec4 FragColor;

void main()
{
    FragColor = vec4(vertexColor, 1.0);
}";

    public static void Initialize(Paper gui, Game game)
    {
        Gizmo3D.gui = gui;
        Gizmo3D.game = game;
        InitializeOpenGL();
    }

    private static void InitializeOpenGL()
    {
        if (_initialized) return;

        // Create VAO
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        // Create VBO
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        // Set up vertex attributes
        // Position attribute (location = 0)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // Color attribute (location = 1)
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // Create and compile shaders
        _shaderProgram = CreateShaderProgram();

        GL.BindVertexArray(0);
        _initialized = true;
    }

    private static int CreateShaderProgram()
    {
        // Compile vertex shader
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, VertexShaderSource);
        GL.CompileShader(vertexShader);
        CheckShaderCompileErrors(vertexShader, "VERTEX");

        // Compile fragment shader
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, FragmentShaderSource);
        GL.CompileShader(fragmentShader);
        CheckShaderCompileErrors(fragmentShader, "FRAGMENT");

        // Create and link shader program
        int program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        CheckProgramLinkErrors(program);

        // Clean up individual shaders
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        return program;
    }

    private static void CheckShaderCompileErrors(int shader, string type)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"Shader compilation error ({type}): {infoLog}");
        }
    }

    private static void CheckProgramLinkErrors(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(program);
            throw new Exception($"Shader program linking error: {infoLog}");
        }
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color = default)
    {
        using var scope = _lock.EnterScope();
        drawCalls.Push(new LineSegment
        {
            Start = start,
            End = end,
            Color = color == default ? Color.White : color,
        });
    }

    public static void DrawBoundingBox(BoundingBox boundingBox, Color color = default)
    {
        var min = (Vector3)boundingBox.Start;
        var max = (Vector3)boundingBox.End;
        var finalColor = color == default ? Color.White : color;

        using var scope = _lock.EnterScope();

        // Bottom face
        drawCalls.Push(new(new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, min.Y, min.Z), finalColor));
        drawCalls.Push(new(new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, min.Y, max.Z), finalColor));
        drawCalls.Push(new(new Vector3(max.X, min.Y, max.Z), new Vector3(min.X, min.Y, max.Z), finalColor));
        drawCalls.Push(new(new Vector3(min.X, min.Y, max.Z), new Vector3(min.X, min.Y, min.Z), finalColor));

        // Top face
        drawCalls.Push(new(new Vector3(min.X, max.Y, min.Z), new Vector3(max.X, max.Y, min.Z), finalColor));
        drawCalls.Push(new(new Vector3(max.X, max.Y, min.Z), new Vector3(max.X, max.Y, max.Z), finalColor));
        drawCalls.Push(new(new Vector3(max.X, max.Y, max.Z), new Vector3(min.X, max.Y, max.Z), finalColor));
        drawCalls.Push(new(new Vector3(min.X, max.Y, max.Z), new Vector3(min.X, max.Y, min.Z), finalColor));

        // Vertical edges
        drawCalls.Push(new(new Vector3(min.X, min.Y, min.Z), new Vector3(min.X, max.Y, min.Z), finalColor));
        drawCalls.Push(new(new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, max.Y, min.Z), finalColor));
        drawCalls.Push(new(new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, max.Y, max.Z), finalColor));
        drawCalls.Push(new(new Vector3(min.X, min.Y, max.Z), new Vector3(min.X, max.Y, max.Z), finalColor));
    }

    public static void Render()
    {
        if (!_initialized || drawCalls.Count == 0) return;

        var camera = game.Camera;
        using var scope = _lock.EnterScope();

        // Convert draw calls to vertex data
        var vertices = new List<float>();
        while (drawCalls.TryPop(out var line))
        {
            var color = line.Color;
            var colorVec = new Vector3(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);

            // Start vertex
            vertices.Add(line.Start.X);
            vertices.Add(line.Start.Y);
            vertices.Add(line.Start.Z);
            vertices.Add(colorVec.X);
            vertices.Add(colorVec.Y);
            vertices.Add(colorVec.Z);

            // End vertex
            vertices.Add(line.End.X);
            vertices.Add(line.End.Y);
            vertices.Add(line.End.Z);
            vertices.Add(colorVec.X);
            vertices.Add(colorVec.Y);
            vertices.Add(colorVec.Z);
        }

        if (vertices.Count == 0) return;

        // Save OpenGL state
        GL.GetInteger(GetPName.DepthTest, out var depthTestEnabled);
        float lineWidth;
        GL.GetFloat(GetPName.LineWidth, out lineWidth);

        // Configure OpenGL state for line rendering
        GL.Enable(EnableCap.DepthTest);
        GL.LineWidth(2.0f);

        // Use our shader program
        GL.UseProgram(_shaderProgram);

        // Calculate view-projection matrix
        var viewMatrix = camera.ViewMatrix;
        var projectionMatrix = camera.ProjectionMatrix;
        var viewProjection = viewMatrix * projectionMatrix;

        int viewProjLocation = GL.GetUniformLocation(_shaderProgram, "uViewProjection");
        GL.UniformMatrix4(viewProjLocation, false, ref viewProjection);

        // Bind VAO and upload vertex data
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.DynamicDraw);

        // Draw lines
        GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Count / 6);

        // Restore OpenGL state
        GL.BindVertexArray(0);
        GL.UseProgram(0);

        if (depthTestEnabled == 0)
            GL.Disable(EnableCap.DepthTest);

        GL.LineWidth(lineWidth);
    }

    public static void Cleanup()
    {
        if (_initialized)
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteProgram(_shaderProgram);
            _initialized = false;
        }
    }
}
