using Caligo.Core.Resources.Block;
using Caligo.ModuleSystem;
using Caligo.ModuleSystem.Runtime;
using Caligo.ModuleSystem.Runtime.Attributes;

namespace Caligo.Core.ModuleSystem.Js;

[JsStaticModule("blocks")]
public class DefineBlock
{
    private Block? _currentBlock;

    public void defineBlock(string name, Action definition)
    {
        var repository = JsModule.CurrentModule.GetStorage<Block>();
        var identifier = Identifier.Create(JsModule.CurrentModule.Identifier, name);
        _currentBlock = new Block
        {
            Name = identifier
        };

        definition();

        repository.Add(
            identifier,
            _currentBlock
        );
    }

    public void defineCubeModel(BlockModelDef modelDefinition)
    {
        if (_currentBlock == null) return;

        var variant = new BlockVariant
        {
            ModelName = "block/block",
            Textures = modelDefinition.Textures ?? [],
            Weight = modelDefinition.Weight ?? 0
        };
        _currentBlock.Variants = _currentBlock.Variants.Append(variant).ToArray();
    }
}