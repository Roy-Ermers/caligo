using System.Reflection;
using Caligo.ModuleSystem.Runtime.Attributes;
using Jint;
using Jint.Native;
using Jint.Runtime.Debugger;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Jint.Runtime.Modules;

namespace Caligo.ModuleSystem.Runtime;

using RegisteredModule = (object module, bool isStatic);

public static class JsEngine
{
    private static Dictionary<string, RegisteredModule>? _registeredModules;

    public static IReadOnlyDictionary<string, RegisteredModule> RegisteredModules =>
        _registeredModules ?? FindGlobalModules();

    private static Dictionary<string, RegisteredModule> FindGlobalModules()
    {
        _registeredModules ??= [];

        var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttributes(typeof(JsModuleAttribute), false).Length > 0 ||
                           type.GetCustomAttributes(typeof(JsStaticModuleAttribute), false).Length > 0);

        foreach (var type in moduleTypes)
        {
            var jsModuleAttribute =
                (JsModuleAttribute?)type.GetCustomAttributes(typeof(JsModuleAttribute), false).FirstOrDefault();
            var jsStaticModuleAttribute =
                (JsStaticModuleAttribute?)type.GetCustomAttributes(typeof(JsStaticModuleAttribute), false)
                    .FirstOrDefault();

            if (jsModuleAttribute is not null)
                _registeredModules[jsModuleAttribute.Name ?? type.Name] = (type, false);
            else if (jsStaticModuleAttribute is not null)
                _registeredModules[jsStaticModuleAttribute.Name ?? type.Name] = (Activator.CreateInstance(type)!, true);
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

            cfg.SetTypeConverter((engine => new ObjectConverter(engine)));
        });

        var console = new NamespacedConsole(identifier);

        runtime.Global.FastSetProperty("console",
            new PropertyDescriptor(JsValue.FromObject(runtime, console), false, true, false));

        foreach (var (name, module) in RegisteredModules)
            runtime.Modules.Add($"@{Identifier.MainModule}/{name}",
                builder => { BuildModule(module, runtime, builder); }
            );

        return runtime;
    }

    private static void BuildModule(RegisteredModule module, Engine engine, ModuleBuilder builder)
    {
        if (!module.isStatic)
        {
            builder.ExportObject("default", module.module);
            return;
        }

        var wrappedInstance = ObjectWrapper.Create(engine, module.module);
        var methods = module.module.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            // 4. Get the function directly from the wrapped object
            // This automatically handles overloads and parameter conversion!
            var jsFunction = wrappedInstance.Get(method.Name);

            if (jsFunction != JsValue.Undefined)
            {
                builder.ExportValue(method.Name, jsFunction);
            }
        }
    }
}