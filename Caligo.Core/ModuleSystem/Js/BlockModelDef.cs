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
    [JsProperty("textures")] public Dictionary<string, object>? Textures { get; set; }
    [JsProperty("weight")] public int? Weight { get; set; }

    public Dictionary<string, string[]> ParsedTextures
    {
        get
        {
            var result = new Dictionary<string, string[]>();

            foreach (var entry in Textures)
            {
                var key = entry.Key;
                var value = entry.Value;

                result.Add(key, value switch
                {
                    string str => new[] { str },
                    object[] arr => [..arr.Select(o => o.ToString() ?? "")],
                    _ => []
                });
            }

            return result;
        }
    }
#pragma warning restore CS8618
#pragma warning restore IDE0052
}