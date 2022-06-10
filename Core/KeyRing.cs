using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Net.Leksi.KeyBox;

public class KeyRing : IKeyRing
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, KeyDefinition> _keyDefinition;
    private readonly string[] _keyNames;

    internal object?[] PrimaryKey { get; set; } = null!;
    
    public object Source { get; internal set; } = null!;

    public object? this[string name] 
    { 
        get 
        {
            if(_keyDefinition[name].Type is Type type)
            {
                return PrimaryKey[_keyDefinition[name].Index];
            }
            return _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(GetTarget(_keyDefinition[name].Path!))?[_keyDefinition[name].KeyFieldName!];
        } 
        set 
        {
            if (_keyDefinition[name].Type is Type type)
            {
                PrimaryKey[_keyDefinition[name].Index] = value;
            } 
            else if (_serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(GetTarget(_keyDefinition[name].Path!)) is IKeyRing keyRing)
            {
                keyRing[_keyDefinition[name].KeyFieldName!] = value;
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
            return PrimaryKey is { } && PrimaryKey.All(v => v is { });
        }
    }

    public IEnumerable<string> Keys => _keyNames;

    public IEnumerable<object?> Values => _keyNames.Select(k => this[k]);

    public int Count => _keyDefinition.Count;

    public IEnumerable<KeyValuePair<string, object?>> Entries =>
        _keyNames.Select(k => new KeyValuePair<string, object?>(k, this[k]));

    internal KeyRing(IServiceProvider serviceProvider, Dictionary<string, KeyDefinition> keyDefinition)
    {
        _serviceProvider = serviceProvider;
        _keyDefinition = keyDefinition;
        _keyNames = _keyDefinition.OrderBy(kv => kv.Value.Index).Select(kv => kv.Key).ToArray();
    }

    public IKeyRing Set(string name, object value)
    {
        this[name] = value;
        return this;
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
