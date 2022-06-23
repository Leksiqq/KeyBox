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
    private IServiceProvider _serviceProvider = null!;

    private KeyBox() { }

    internal static void AddKeyBox(IServiceCollection services, Action<IKeyBoxConfiguration> configure)
    {
        KeyBox instance = new();
        configure?.Invoke(instance);
        instance.Commit(services);
        services.AddSingleton<IKeyBox>(services => 
        {
            instance._serviceProvider = services;
            return instance;
        });
    }


    bool IKeyBox.HasMappedPrimaryKeys<T>()
    {
        return ((IKeyBox)this).HasMappedPrimaryKeys(typeof(T));
    }

    bool IKeyBox.HasMappedPrimaryKeys(Type type)
    {
        return _primaryKeysMap.ContainsKey(type)
            || type.IsInterface && _primaryKeysMap.Keys.Where(t => type.IsAssignableFrom(t)).FirstOrDefault() is Type;
    }

    IKeyRing? IKeyBox.GetKeyRing(object? source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        lock(source)
        {
            KeyRing? keyRing = null;
            if (_attachedKeys.TryGetValue(source, out keyRing))
            {
                return keyRing;
            }
            Type? type = source.GetType();
            if (_primaryKeysMap.ContainsKey(type))
            {
                keyRing = new KeyRing(_serviceProvider, _primaryKeysMap[type], source);
                _attachedKeys.Add(source, keyRing);
            }
            return keyRing;
        }
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
            if (definition[name] is Type type)
            {
                definitions[name] = new KeyDefinition { Type = type };
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
                        else if (parts.Length < 3)
                        {
                            throw new ArgumentException($"{nameof(definition)} path for name {name} must have at least 2 parts");
                        }
                        else
                        {
                            definitions[name] = new KeyDefinitionByKey { KeyFieldName = parts[i] };
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
                if(!definitions.ContainsKey(name))
                {
                    definitions[name] = new KeyDefinitionByProperty { PropertiesPath = propertyInfos.ToArray(), Type = current };
                }
                (definitions[name] as KeyDefinitionByProperty).PropertiesPath = propertyInfos.ToArray();
            }
            else
            {
                throw new ArgumentException($"{nameof(definition)} values can be Types or strings");
            }
            definitions[name].Index = pos;
            ++pos;
        }
        _primaryKeysMap[targetType] = definitions;
        return this;
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
        Stack<KeyDefinition> stack = new();
        List<Type> toRemove = new();
        foreach (KeyValuePair<Type, Dictionary<string, KeyDefinition>> definitions in _primaryKeysMap)
        {
            foreach (KeyValuePair<string, KeyDefinition> definition in definitions.Value)
            {
                if (definition.Value is KeyDefinitionByKey definitionByKey)
                {
                    if (
                        !_primaryKeysMap.ContainsKey(definitionByKey.PropertiesPath!.Last().PropertyType) 
                        || !_primaryKeysMap[definitionByKey.PropertiesPath!.Last().PropertyType].ContainsKey(definitionByKey.KeyFieldName!)
                    )
                    {
                        toRemove.Add(definitions.Key);
                        exceptions.Add(new ArgumentException($"The {nameof(definitionByKey.PropertiesPath)} at {nameof(IKeyBoxConfiguration.AddPrimaryKey)} "
                        + $"for type {definitions.Key} and name {definition.Key} is invalid as primary key field {definitionByKey.KeyFieldName} "
                        + $"is not defined for {definitionByKey.PropertiesPath!.Last().PropertyType}"));
                    }
                    else if(definitionByKey.Type is null)
                    {
                        KeyDefinition current = definitionByKey;
                        do
                        {
                            stack.Push(current);
                            if(current is not KeyDefinitionByKey)
                            {
                                break;
                            }
                            current = _primaryKeysMap[(current as KeyDefinitionByKey)!.PropertiesPath!.Last().PropertyType][(current as KeyDefinitionByKey)!.KeyFieldName!];
                        }
                        while (current.Type is null);
                        while(stack.Count > 0)
                        {
                            stack.Pop().Type = current.Type;
                        }
                    }
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
