using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Net.Leksi.KeyBox;

internal class KeyRing : IKeyRing
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, KeyDefinition> _keyDefinition;
    private readonly string[] _keyNames;

    private readonly object?[] _primaryKey;
    
    public object Source { get; private set; } = null!;

    public object? this[string name] 
    { 
        get 
        {
            if (_keyDefinition[name] is KeyDefinitionByKey definitionByKey)
            {
                return _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(GetTarget(definitionByKey.PropertiesPath!))?[definitionByKey.KeyFieldName!];
            }
            if (_keyDefinition[name] is KeyDefinitionByProperty definitionByProperty)
            {
                return GetValue(definitionByProperty.PropertiesPath!);
            }
            return _primaryKey[_keyDefinition[name].Index];
        }
        set 
        {
            if(_keyDefinition[name] is KeyDefinitionByKey definitionByKey)
            {
                if (_serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(GetTarget(definitionByKey.PropertiesPath!)) is IKeyRing keyRing)
                {
                    keyRing[definitionByKey.KeyFieldName!] = value;
                }
            }
            else if (_keyDefinition[name] is KeyDefinitionByProperty definitionByProperty)
            {
                SetValue(definitionByProperty.PropertiesPath!, value);
            }
            else
            {
                _primaryKey[_keyDefinition[name].Index] = value;
            }
        }
    }

    public object? this[int index]
    {
        get
        {
            return this[_keyNames[index]];
        }
        set
        {
            this[_keyNames[index]] = value;
        }
    }

    public bool IsCompleted
    {
        get
        {
            return _primaryKey is { } && _primaryKey.All(v => v is { });
        }
    }

    public IEnumerable<string> Keys => _keyNames;

    public IEnumerable<object?> Values => _keyNames.Select(k => this[k]);

    public int Count => _keyDefinition.Count;

    public IEnumerable<KeyValuePair<string, object?>> Entries =>
        _keyNames.Select(k => new KeyValuePair<string, object?>(k, this[k]));

    internal KeyRing(IServiceProvider serviceProvider, Dictionary<string, KeyDefinition> keyDefinition, object source)
    {
        Source = source;
        _serviceProvider = serviceProvider;
        _keyDefinition = keyDefinition;
        _keyNames = _keyDefinition.OrderBy(kv => kv.Value.Index).Select(kv => kv.Key).ToArray();
        _primaryKey = new object[keyDefinition.Count];
    }

    public IKeyRing Set(string name, object value)
    {
        this[name] = value;
        return this;
    }

    public Type GetPartType(int index)
    {
        return _keyDefinition[_keyNames[index]].Type!;
    }

    private object? GetValue(PropertyInfo[] path)
    {
        object? target = Source;
        for (int i = 0; i < path.Length; ++i)
        {
            object? next = path[i].GetValue(target);
            if (next is null && i < path.Length - 1)
            {
                next = _serviceProvider.GetRequiredService(path[i].PropertyType);
                path[i].SetValue(target, next);
            }
            target = next;
        }
        return target;
    }

    private void SetValue(PropertyInfo[] path, object? value)
    {
        object? target = Source;
        for (int i = 0; i < path.Length; ++i)
        {
            if (i < path.Length - 1)
            {
                object? next = path[i].GetValue(target);
                if (next is null)
                {
                    next = _serviceProvider.GetRequiredService(path[i].PropertyType);
                    path[i].SetValue(target, next);
                }
                target = next;
            }
            else
            {
                path[i].SetValue(target, value);
            }
        }
    }

    private object GetTarget(PropertyInfo[] path)
    {
        object? target = Source;
        for (int i = 0; i < path.Length; ++i)
        {
            object? next = path[i].GetValue(target);
            if (next is null)
            {
                next = _serviceProvider.GetRequiredService(path[i].PropertyType);
                path[i].SetValue(target, next);
            }
            target = next;
        }
        return target;
    }
}
