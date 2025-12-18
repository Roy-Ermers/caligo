using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Reflection;
using Jint;
using Jint.Native;
using Jint.Runtime.Interop;

namespace Caligo.ModuleSystem.Runtime;

public class ObjectConverter : ITypeConverter
{
    private readonly ITypeConverter _defaultConverter;
#if NETSTANDARD
        private static readonly ConcurrentDictionary<(Type Source, Type Target), bool> _knownConversions =
 new ConcurrentDictionary<(Type Source, Type Target), bool>();
#else
    private static readonly ConcurrentDictionary<string, bool> _knownConversions =
        new ConcurrentDictionary<string, bool>();
#endif

    private static readonly Type nullableType = typeof(Nullable<>);
    private static readonly Type intType = typeof(int);
    private static readonly Type iCallableType = typeof(Func<JsValue, JsValue[], JsValue>);
    private static readonly Type jsValueType = typeof(JsValue);
    private static readonly Type objectType = typeof(object);
    private static readonly Type engineType = typeof(Engine);
    private static readonly Type typeType = typeof(Type);

    private static readonly MethodInfo convertChangeType =
        typeof(Convert).GetMethod("ChangeType", new[] { objectType, typeType, typeof(IFormatProvider) });

    private static readonly MethodInfo jsValueFromObject = jsValueType.GetMethod(nameof(JsValue.FromObject));
    private static readonly MethodInfo jsValueToObject = jsValueType.GetMethod(nameof(JsValue.ToObject));

    public ObjectConverter(Engine engine)
    {
        _defaultConverter = new DefaultTypeConverter(engine);
    }

    public object? Convert(object? value, Type targetType, IFormatProvider formatProvider)
    {
        if (value is ExpandoObject)
        {
            var dict = (IDictionary<string, object>)value;

            // 1. Handle Dictionary Support
            // This allows Dictionary<string, T> to work
            if (typeof(IDictionary).IsAssignableFrom(targetType) && targetType.IsGenericType)
            {
                var instance = (IDictionary)Activator.CreateInstance(targetType);
                var valueType = targetType.GetGenericArguments()[1]; // The 'TValue' in Dictionary<TKey, TValue>

                foreach (var entry in dict)
                {
                    // Recursively convert the value in case it's another object/dictionary
                    var convertedValue = Convert(entry.Value, valueType, formatProvider);
                    instance.Add(entry.Key, convertedValue);
                }

                return instance;
            }

            var constructors = targetType.GetConstructors();
            if (targetType.IsValueType && constructors.Length > 0) return null;

            // Ensure there is a parameterless constructor
            var hasDefaultConstructor = false;
            foreach (var constructor in constructors)
            {
                if (constructor.GetParameters().Length == 0 && constructor.IsPublic)
                {
                    hasDefaultConstructor = true;
                    break;
                }
            }

            if (!hasDefaultConstructor && !targetType.IsValueType) return null;

            var obj = Activator.CreateInstance(targetType, Array.Empty<object>());
            var members = targetType.GetMembers(BindingFlags.Public | BindingFlags.Instance);

            foreach (var member in members)
            {
                if (member.MemberType != MemberTypes.Property && member.MemberType != MemberTypes.Field)
                    continue;

                // Try matching the name directly (textures) or with camelCase conversion
                var name = member.Name;
                var camelName = name.Length > 1
                    ? char.ToLowerInvariant(name[0]) + name.Substring(1)
                    : name.ToLowerInvariant();

                if (dict.TryGetValue(name, out var val) || dict.TryGetValue(camelName, out val))
                {
                    var memberType = member is PropertyInfo p ? p.PropertyType : ((FieldInfo)member).FieldType;
                    var output = Convert(val, memberType, formatProvider);
                    if (member is PropertyInfo propertyInfo)
                    {
                        if (propertyInfo.CanWrite)
                            propertyInfo.SetValue(obj, output);
                    }
                    else if (member is FieldInfo fieldInfo)
                    {
                        fieldInfo.SetValue(obj, output);
                    }
                }
            }

            return obj;
        }


        // Fall back to Jint's default conversion logic for everything else
        return _defaultConverter.Convert(value, targetType, formatProvider);
    }

    public bool TryConvert(object? value, Type type, IFormatProvider formatProvider,
        [NotNullWhen(true)] out object? converted)
    {
        converted = Convert(value, type, formatProvider);
        return converted != null;
    }
}