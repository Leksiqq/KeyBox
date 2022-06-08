using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Net.Leksi.KeyBox;

internal class KeyBox : IKeyBox, IKeyBoxConfiguration
{
    private readonly Dictionary<Type, Dictionary<string, KeyDefinition>> _primaryKeysMap = new();
    private readonly Dictionary<Type, Type> _exampleKeyMap = new();
    private readonly ConditionalWeakTable<object, KeyRing> _attachedKeys = new();
    private readonly ConcurrentDictionary<Type, Type?> _mappedTypesCache = new();
    private bool _isConfigured = false;

    public bool HasMappedPrimaryKeys => _primaryKeysMap.Count > 0;

    private KeyBox() { }

    internal static void AddKeyBox(IServiceCollection services, Action<IKeyBoxConfiguration> configure)
    {
        KeyBox instance = new();
        configure?.Invoke(instance);
        instance.Commit();
        services.AddSingleton<IKeyBox>(instance);
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

    IKeyBoxConfiguration IKeyBoxConfiguration.AddPrimaryKey(Type targetType, IDictionary<string, Type> definition)
    {
        ThrowIfConfigured();
        ThrowIfNotClass(nameof(targetType), targetType);
        ThrowIfAlreadyMapped(targetType);
        _primaryKeysMap[targetType] = new Dictionary<string, KeyDefinition>();
        int pos = 0;
        foreach (string name in definition.Keys.OrderBy(v => v))
        {
            _primaryKeysMap[targetType][name] = new KeyDefinition { Index = pos, Type = definition[name] };
            ++pos;
        }
        return this;
    }

    IKeyBoxConfiguration IKeyBoxConfiguration.AddPrimaryKey(Type targetType, Type exampleType)
    {
        ThrowIfConfigured();
        ThrowIfNotClass(nameof(targetType), targetType);
        ThrowIfNotClass(nameof(exampleType), exampleType);
        ThrowIfAlreadyMapped(targetType);
        _exampleKeyMap[targetType] = exampleType;
        return this;
    }

    IKeyBoxConfiguration IKeyBoxConfiguration.AddForeignKey(Type targetType, IDictionary<string, Type> definition)
    {
        throw new NotImplementedException();
    }

    IKeyBoxConfiguration IKeyBoxConfiguration.AddForeignKey(Type targetType, Type exampleType)
    {
        throw new NotImplementedException();
    }

    internal void CreateKeyRing(object source)
    {
        KeyRing? keyRing = null;
        if (!_attachedKeys.TryGetValue(source, out keyRing))
        {
            Type? mapped = GetMappedType(source.GetType());
            if (mapped is { })
            {
                keyRing = new KeyRing(_primaryKeysMap[mapped]);
                keyRing.PrimaryKey = new object[_primaryKeysMap[mapped].Count];
                keyRing.Source = source;
                _attachedKeys.Add(source, keyRing);
            }
        }
    }

    private Type? GetMappedType(Type actualType)
    {
        Type? mapped = null;
        if (!_mappedTypesCache.TryGetValue(actualType, out mapped))
        {
            Type? current = actualType;
            while (current is { } && !_primaryKeysMap.ContainsKey(current))
            {
                current = current!.BaseType;
            }
            mapped = current;
            _mappedTypesCache.TryAdd(actualType, mapped);
        }
        return mapped;
    }

    private void Commit()
    {
        MapExamples();
        _isConfigured = true;
    }

    private void ThrowIfAlreadyMapped(Type targetType)
    {
        if (_primaryKeysMap.ContainsKey(targetType) || _exampleKeyMap.ContainsKey(targetType))
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

    private void MapExamples()
    {
        if (_exampleKeyMap.Count > 0)
        {
            List<Type> notMapped = new();
            Stack<Type> stack = new();
            while (_exampleKeyMap.Count > 0)
            {
                if (stack.Count == 0)
                {
                    stack.Push(_exampleKeyMap.Keys.First());
                }
                if (!_primaryKeysMap.ContainsKey(stack.Peek()) && !_exampleKeyMap.ContainsKey(stack.Peek()))
                {
                    while (stack.Count > 0)
                    {
                        _exampleKeyMap.Remove(stack.Peek());
                        notMapped.Add(stack.Pop());
                    }
                }
                else if (_primaryKeysMap.ContainsKey(stack.Peek()))
                {
                    Dictionary<string, KeyDefinition> example = _primaryKeysMap[stack.Peek()];
                    while (stack.Count > 0)
                    {
                        _exampleKeyMap.Remove(stack.Peek());
                        _primaryKeysMap[stack.Pop()] = example;
                    }
                }
                else
                {
                    if (stack.Contains(_exampleKeyMap[stack.Peek()]))
                    {
                        throw new Exception($"Example loop detected: {_exampleKeyMap[stack.Peek()]}");
                    }
                    stack.Push(_exampleKeyMap[stack.Peek()]);
                }
            }
            if (stack.Count > 0)
            {
                notMapped.AddRange(stack);
            }
            if (notMapped.Count > 0)
            {
                List<string> list = notMapped.Select(t => t.ToString()).OrderBy(s => s).ToList();
                for (int i = list.Count - 1; i > 0; --i)
                {
                    if (list[i - 1] == list[i])
                    {
                        list.RemoveAt(i);
                    }
                }
                throw new Exception($"Keys not mapped for: {string.Join(", ", list)}");
            }
        }
    }
}
