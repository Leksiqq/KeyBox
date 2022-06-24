using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Net.Leksi.KeyBox;

internal class KeyRing : IKeyRing
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, KeyDefinition> _keyDefinition;
    private readonly string[] _keyNames;
    private readonly Type _sourceType;
    private readonly KeyBox _keyBox;
    private readonly object _lock = new();
    private object? _source = null;

    private readonly object?[] _primaryKey;
    
    public object? Source { 
        get 
        { 
            lock(_lock)
            {
                return _source;
            }
        } 
    }

    public object? this[string name] 
    { 
        get 
        {
            if (Source is { })
            {
                if (_keyDefinition[name] is KeyDefinitionByKey definitionByKey)
                {
                    return _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(GetTarget(definitionByKey.PropertiesPath!))?[definitionByKey.KeyFieldName!];
                }
                if (_keyDefinition[name] is KeyDefinitionByProperty definitionByProperty)
                {
                    return GetValue(definitionByProperty.PropertiesPath!);
                }
            }
            return _primaryKey[_keyDefinition[name].Index];
        }
        set 
        {
            if(Source is { } && _keyDefinition[name] is KeyDefinitionByKey definitionByKey)
            {
                if (_serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(GetTarget(definitionByKey.PropertiesPath!)) is IKeyRing keyRing)
                {
                    keyRing[definitionByKey.KeyFieldName!] = value;
                }
            }
            else if (Source is { } && _keyDefinition[name] is KeyDefinitionByProperty definitionByProperty)
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

    internal KeyRing(IServiceProvider serviceProvider, KeyBox keyBox, Type type, Dictionary<string, KeyDefinition> keyDefinition, object? source)
    {
        _sourceType = type;
        _source = source;
        _serviceProvider = serviceProvider;
        _keyDefinition = keyDefinition;
        _keyNames = _keyDefinition.OrderBy(kv => kv.Value.Index).Select(kv => kv.Key).ToArray();
        _primaryKey = new object[keyDefinition.Count];
        _keyBox = keyBox;
    }

    public override int GetHashCode()
    {
        return Values.Select(v => v is null ? 0 : v.GetHashCode()).Aggregate(0, (v, res) => unchecked(v + res * 7));
    }

    public override bool Equals(object? obj)
    {
        if(obj is KeyRing another)
        {
            return Count == another.Count && Values.Zip(another.Values).All(v => v.First is { } && v.Second is { } && v.First.Equals(v.Second));
        }
        return false;
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


    public object InstantiateSource()
    {
        if(_source is null)
        {
            lock(_lock)
            {
                if (_source is null)
                {
                    _source = _serviceProvider.GetRequiredService(_sourceType);
                    foreach (string name in Keys)
                    {
                        if (_keyDefinition[name] is KeyDefinitionByKey || _keyDefinition[name] is KeyDefinitionByProperty)
                        {
                            this[name] = _primaryKey[_keyDefinition[name].Index];
                        }
                    }
                    _keyBox.AttachKeyRing(_source, this);
                }
            }
        }
        return _source;
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
