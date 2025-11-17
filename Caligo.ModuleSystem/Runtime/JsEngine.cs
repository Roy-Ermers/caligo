using Caligo.ModuleSystem.Runtime.Attributes;
using Jint;
using Jint.Native;
using Jint.Runtime.Debugger;
using Jint.Runtime.Descriptors;

namespace Caligo.ModuleSystem.Runtime;

public static class JsEngine
{
    private static Dictionary<string, Type>? _registeredModules;

    public static IReadOnlyDictionary<string, Type> RegisteredModules => _registeredModules ?? FindGlobalModules();

    private static Dictionary<string, Type> FindGlobalModules()
    {
        _registeredModules ??= [];
        
        var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttributes(typeof(JsModuleAttribute), false).Length > 0);

        foreach (var type in moduleTypes)
        {
            var attribute = (JsModuleAttribute)type.GetCustomAttributes(typeof(JsModuleAttribute), false).First();
            _registeredModules[attribute.Name ?? type.Name] = type;
        }

        return _registeredModules;
    }
    
    public static Engine CreateEngine(string identifier, string absoluteDirectory)
    {
        var runtime = new Engine(cfg =>
        {
            cfg.AllowOperatorOverloading()
                .DebuggerStatementHandling(DebuggerStatementHandling.Clr)
                .LimitMemory(32_000_000) // 32 MB
                .LimitRecursion(256)
                .Strict()
                .EnableModules(absoluteDirectory);
        });

        var console = new NamespacedConsole(identifier);
        
        runtime.Global.FastSetProperty("console", new PropertyDescriptor(JsValue.FromObject(runtime, console), false, true, false));

        foreach (var (name,module) in RegisteredModules)
        {
            runtime.Modules.Add($"@{Identifier.MainModule}/{name}", builder => builder.ExportType("default", module));
        }

        return runtime;
    }
}