using OpenTK.Graphics.OpenGL4;

namespace WorldGen.Graphics.Shaders;

public class ComputeShader(string computeShaderPath) :
    BaseShader(
        Path.GetDirectoryName(computeShaderPath)!,
        Path.GetFileNameWithoutExtension(computeShaderPath)
    )
{
    protected override void InitializeShaders()
    {
        LoadShader(ShaderType.ComputeShader, computeShaderPath);
    }

    public void Dispatch(int x, int y, int z)
    {
        if (!ProgramHandle.HasValue)
            throw new Exception("Shader program not initialized, please call Initialize() first");

        GL.UseProgram(ProgramHandle.Value);
        GL.DispatchCompute(x, y, z);
    }
}
