namespace Caligo.Core.FileSystem;

public record class Font(string FilePath)
{
    public string FilePath { get; init; } = FilePath;
}
