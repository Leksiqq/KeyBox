using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Net.Leksi.KeyBox;

public class KeyRing : IKeyRing
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
            if(_keyDefinition[name].Type is Type type)
            {
                return _primaryKey[_keyDefinition[name].Index];
            }
            if (_keyDefinition[name].KeyFieldName is { })
            {
                return _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(GetTarget(_keyDefinition[name].Path!))?[_keyDefinition[name].KeyFieldName!];
            }
            return GetValue(_keyDefinition[name].Path!);
        } 
        set 
        {
            if (_keyDefinition[name].Type is Type type)
            {
                _primaryKey[_keyDefinition[name].Index] = value;
            } 
            else if(_keyDefinition[name].KeyFieldName is { })
            {
                if (_serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(GetTarget(_keyDefinition[name].Path!)) is IKeyRing keyRing)
                {
                    keyRing[_keyDefinition[name].KeyFieldName!] = value;
                }
            }
            else
            {
                SetValue(_keyDefinition[name].Path!, value);
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

    public IEnumerable<object?> PrimaryKey => _primaryKey.Select(v => v);

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
        _primaryKey = new object[_keyNames.Length];
    }

    public IKeyRing Set(string name, object value)
    {
        this[name] = value;
        return this;
    }

    private object? GetValue(PropertyInfo[] path)
    {
        object? target = Source;
        object? value;
        for (int i = 0; i < path.Length; ++i)
        {
            if (i < path.Length - 1)
            {
                value = path[i].GetValue(target, null);
                if (value is null)
                {
                    value = _serviceProvider.GetRequiredService(path[i].PropertyType);
                    path[i].SetValue(target, value);
                }
                target = value;
            }
            else
            {
                target = path[i].GetValue(target);
            }
        }
        return target;
    }

    private void SetValue(PropertyInfo[] path, object? value)
    {
        object? target = Source;
        object? value1;
        for (int i = 0; i < path.Length; ++i)
        {
            if (i < path.Length - 1)
            {
                value1 = path[i].GetValue(target, null);
                if (value1 is null)
                {
                    value1 = _serviceProvider.GetRequiredService(path[i].PropertyType);
                    path[i].SetValue(target, value1);
                }
                target = value1;
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
        object? value;
        foreach (PropertyInfo propertyInfo in path)
        {
            value = propertyInfo.GetValue(target, null);
            if (value is null)
            {
                value = _serviceProvider.GetRequiredService(propertyInfo.PropertyType);
                propertyInfo.SetValue(target, value);
            }
            target = value;
        }
        return target;
    }

}
