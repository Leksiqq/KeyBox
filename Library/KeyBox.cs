using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Net.Leksi.KeyBox;

internal class KeyBox : IKeyBox, IKeyBoxConfiguration
{
    private const string _nullableAttributeName = "NullableAttribute";

    private readonly Dictionary<Type, Dictionary<string, KeyDefinition>> _primaryKeysMap = new();
    private readonly ConditionalWeakTable<object, KeyRing> _attachedKeys = new();
    private bool _isConfigured = false;

    public bool HasMappedPrimaryKeys => _primaryKeysMap.Count > 0;

    private KeyBox() { }

    internal static void AddKeyBox(IServiceCollection services, Action<IKeyBoxConfiguration> configure)
    {
        KeyBox instance = new();
        configure?.Invoke(instance);
        instance.Commit(services);
        services.AddSingleton<IKeyBox>(instance);
    }

    internal static void AddKeyBox(IHostBuilder builder, Action<IKeyBoxConfiguration> configure)
    {
        builder.UseServiceProviderFactory(new ServiceProviderFactory()).ConfigureServices((context, services) =>
        {
            AddKeyBox(services, configure);
        });
    }

    IKeyRing? IKeyBox.GetKeyRing(object? source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        KeyRing? keyRing = null;
        if (_attachedKeys.TryGetValue(source, out keyRing))
        {
            return keyRing;
        }
        return null;
    }

    IKeyBoxConfiguration IKeyBoxConfiguration.AddPrimaryKey(Type targetType, IDictionary<string, object> definition)
    {
        ThrowIfConfigured();
        ThrowIfNotClass(nameof(targetType), targetType);
        ThrowIfAlreadyMapped(targetType);
        Dictionary<string, KeyDefinition> definitions = new Dictionary<string, KeyDefinition>();
        int pos = 0;
        foreach (string name in definition.Keys.OrderBy(v => v))
        {
            definitions[name] = new KeyDefinition { Index = pos };
            if (definition[name] is Type type)
            {
                definitions[name].Type = type;
            }
            else if (definition[name] is string path)
            {
                string[] parts = path.Split('/');
                if (!string.Empty.Equals(parts[0]))
                {
                    throw new ArgumentException($"{nameof(definition)} path for name {name} must start with /");
                }
                List<PropertyInfo> propertyInfos = new();
                Type current = targetType;
                for (int i = 1; i < parts.Length; ++i)
                {
                    PropertyInfo? propertyInfo = current.GetProperty(parts[i]);
                    if (propertyInfo is null)
                    {
                        if(i < parts.Length - 1)
                        {
                            throw new ArgumentException($"{nameof(definition)} path for name {name} has invalid part: {parts[i]}");
                        }
                        else
                        {
                            definitions[name].KeyFieldName = parts[i];
                        }
                    } 
                    else
                    {
                        NullabilityInfoContext nullabilityInfo = new NullabilityInfoContext();
                        var nullability = nullabilityInfo.Create(propertyInfo);
                        if (nullability.WriteState is NullabilityState.Nullable)
                        {
                            throw new ArgumentException($"{nameof(definition)} path for name {name} has nullable part: {parts[i]}");
                        }
                        current = propertyInfo.PropertyType;
                        propertyInfos.Add(propertyInfo);
                    }
                }
                definitions[name].Path = propertyInfos.ToArray();
            }
            else
            {
                throw new ArgumentException($"{nameof(definition)} values can be Types or strings");
            }
            ++pos;
        }
        _primaryKeysMap[targetType] = definitions;
        return this;
    }

    internal void CreateKeyRing(IServiceProvider serviceProvider, object source)
    {
        KeyRing? keyRing = null;
        if (!_attachedKeys.TryGetValue(source, out keyRing))
        {
            Type? type = source.GetType();
            if (_primaryKeysMap.ContainsKey(type))
            {
                keyRing = new KeyRing(serviceProvider, _primaryKeysMap[type], source);
                _attachedKeys.Add(source, keyRing);
            }
        }
    }

    private void Commit(IServiceCollection services)
    {
        List<Exception> exceptions = new();
        CheckPaths(exceptions);
        if(exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }
        foreach (Type type in _primaryKeysMap.Keys)
        {
            if(services.Where(item => item.ServiceType == type).Count() == 0)
            {
                services.AddTransient(type);
            }
        }
        _isConfigured = true;
    }

    private void CheckPaths(List<Exception> exceptions)
    {
        List<Type> toRemove = new();
        foreach (KeyValuePair<Type, Dictionary<string, KeyDefinition>> definitions in _primaryKeysMap)
        {
            foreach (KeyValuePair<string, KeyDefinition> definition in definitions.Value)
            {
                if (definition.Value.Path is PropertyInfo[] path)
                {
                    Console.WriteLine(String.Join(", ", path.Select(p => p.ToString())) + ", " + definition.Value.KeyFieldName);
                    //if (
                    //    !(!_primaryKeysMap.ContainsKey(path.Last().PropertyType) && definition.Value.KeyFieldName is null)
                    //    || !_primaryKeysMap.ContainsKey(path.Last().PropertyType) || !_primaryKeysMap[path.Last().PropertyType].ContainsKey(definition.Value.KeyFieldName!))
                    //{
                    //    toRemove.Add(definitions.Key);
                    //    exceptions.Add(new ArgumentException($"The {nameof(KeyDefinition.Path)} at {nameof(IKeyBoxConfiguration.AddPrimaryKey)} "
                    //    + $"for type {definitions.Key} and name {definition.Key} is invalid as primary key field {definition.Value.KeyFieldName} " 
                    //    + $"is not defined for {path.Last().PropertyType}"));
                    //}
                }
            }
        }
        toRemove.ForEach(k => _primaryKeysMap.Remove(k));
    }

    private void ThrowIfAlreadyMapped(Type targetType)
    {
        if (_primaryKeysMap.ContainsKey(targetType))
        {
            throw new InvalidOperationException($"Key for {targetType} is already mapped");
        }
    }

    private static void ThrowIfNotClass(string argName, Type targetType)
    {
        if (targetType.IsInterface || targetType.IsValueType)
        {
            throw new ArgumentException($"{argName} must be a class");
        }
    }

    private void ThrowIfConfigured()
    {
        if (_isConfigured)
        {
            throw new InvalidOperationException($"{typeof(IKeyBox)} is already configured");
        }
    }

}
