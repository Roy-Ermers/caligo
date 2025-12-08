using System.Text.Json.Serialization;

namespace Caligo.Core.Resources.Block.Models;

public class BlockModel
{
    [JsonPropertyName("parent")] public string? ParentName { get; set; }

    [JsonIgnore] public BlockModel? Parent { get; set; }

    [JsonPropertyName("offsetType")] public string? OffsetType { get; set; }

    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cullFaces")] public ModelCulling? Culling { get; set; }

    public BlockModelCube[] Elements { get; set; } = [];

    public Dictionary<string, string>? Textures { get; set; }

    [JsonIgnore] public bool IsBuilt { get; private set; }

    /// <summary>
    ///     Collapses the block model by merging all parent data into this model.
    ///     This is useful for resolving inheritance in block models.
    /// </summary>
    public void Build()
    {
        if (IsBuilt)
            return;

        IsBuilt = true;

        if (Parent is not null)
        {
            Parent.Build();

            Culling ??= Parent.Culling;
            OffsetType ??= Parent.OffsetType;

            if (Elements.Length == 0) Elements = ImportElements(Parent.Elements, Textures);
        }
    }

    private static BlockModelCube[] ImportElements(BlockModelCube[] elements, Dictionary<string, string>? textures)
    {
        if (textures is null)
            return elements;

        var newElements = new BlockModelCube[elements.Length];

        for (var i = 0; i < elements.Length; i++)
        {
            var newTextureFaces = elements[i].TextureFaces.SetTextureVariables(textures);
            var element = new BlockModelCube
            {
                From = elements[i].From,
                To = elements[i].To,
                TextureFaces = newTextureFaces
            };

            newElements[i] = element;
        }

        return newElements;
    }

    private BlockModel Clone()
    {
        var copy = new BlockModel
        {
            ParentName = ParentName,
            OffsetType = OffsetType,
            Name = Name,
            Culling = Culling,
            Elements = (BlockModelCube[])Elements.Clone(),
            Textures = Textures?.ToDictionary(entry => entry.Key, entry => entry.Value)
        };

        if (Parent is not null)
            copy.Parent = Parent.Clone();

        return copy;
    }

    public override string ToString()
    {
        var result = $"BlockModel {Name}";

        if (Parent is not null) result += $" (Parent: {Parent.Name})";

        if (Culling != ModelCulling.None) result += $" (Culling: {Culling})";

        if (Textures is not null && Textures.Count > 0)
            result += $" (Textures: {string.Join(", ", Textures.Select(t => $"{t.Key}={t.Value}"))})";

        if (Elements.Length > 0)
            result += $" (Elements: {Elements.Length})";
        else
            result += " (No elements)";


        return result;
    }
}