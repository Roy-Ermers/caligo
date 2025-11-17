namespace WorldGen.Renderer.Shaders;

public class RenderShader(string vertexShaderPath, string fragmentShaderPath)
    : BaseShader(
        Path.GetDirectoryName(vertexShaderPath)!,
        Path.GetFileNameWithoutExtension(vertexShaderPath)
    )
{
    protected override void InitializeShaders()
    {
        LoadShader(ShaderType.VertexShader, vertexShaderPath);
        LoadShader(ShaderType.FragmentShader, fragmentShaderPath);
    }
}
