using Caligo.ModuleSystem.Runtime.Attributes;

namespace Caligo.Core.ModuleSystem.Js;

/// <summary>
/// Represents a block model definition passed from JavaScript.
/// Jint will automatically marshal JS objects to this class.
/// </summary>
[JsConvertible]
public class BlockModelDef
{
#pragma warning disable CS8618 // Non-nullable field is uninitialized
#pragma warning disable IDE0052 // Remove unread private member
    [JsProperty("textures")] public Dictionary<string, string>? Textures { get; set; }
    [JsProperty("weight")] public int? Weight { get; set; }
#pragma warning restore CS8618
#pragma warning restore IDE0052
}