namespace WorldGen.ModuleSystem;

public static class Identifier
{
  public const string MainModule = "main";

  /// <summary>
  /// Resolves an identifier to a namespaced identifier
  /// </summary>
  /// <param name="identifier">the identifier to resolve.</param>
  /// <returns>A namespaced identifier</returns>
  /// <exception cref="Exception">Happens when the identifier is empty</exception>
  public static string Resolve(string identifier) => Resolve(identifier, MainModule);

  /// <summary>
  /// Resolves an identifier to a namespaced identifier
  /// </summary>
  /// <param name="identifier">the identifier to resolve.</param>
  /// <param name="fallbackModule">The module namespace to fallback to.</param>
  /// <returns>A namespaced identifier</returns>
  /// <exception cref="Exception">Happens when the identifier is empty</exception>
  public static string Resolve(string identifier, string fallbackModule)
  {
    var parts = identifier.Split(':');

    return parts.Length switch
    {
      1 => $"{fallbackModule}:{parts[0]}",
      2 => identifier,
      _ => throw new Exception($"Invalid identifier: {identifier}")
    };
  }

  /// <summary>
  /// Extracts the module from an identifier
  /// </summary>
  /// <param name="identifier">The identifier to extract the module name from.</param>
  /// <returns>The module name</returns>
  /// <exception cref="Exception"></exception>
  public static string ResolveModule(string identifier)
  {
    var parts = identifier.Split(':');

    return parts.Length switch
    {
      1 => MainModule,
      2 => parts[0],
      _ => throw new Exception($"Invalid identifier: {identifier}")
    };
  }

  /// <summary>
  /// Extracts the name from an identifier
  /// </summary>
  /// <param name="identifier"></param>
  /// <returns></returns>
  /// <exception cref="Exception"></exception>
  public static string ResolveName(string identifier)
  {
    var parts = identifier.Split(':');

    return parts.Length switch
    {
      1 => parts[0],
      2 => parts[1],
      _ => throw new Exception($"Invalid identifier: {identifier}")
    };
  }

  public static (string module, string name) Parse(string identifier)
  {
    var parts = identifier.Split(':');

    return parts.Length switch
    {
      1 => (MainModule, parts[0]),
      2 => (parts[0], parts[1]),
      _ => throw new Exception($"Invalid identifier: {identifier}")
    };
  }

  public static string Create(string module, string name)
  {
    return $"{module}:{name}";
  }

  public static string Create(string name)
  {
    return Create(MainModule, name);
  }
}
