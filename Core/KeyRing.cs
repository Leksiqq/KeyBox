using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Net.Leksi.KeyBox;

public class KeyRing : IKeyRing
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, KeyDefinition> _keyDefinition;

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

    public bool IsCompleted
    {
        get
        {
            return PrimaryKey is { } && PrimaryKey.All(v => v is { });
        }
    }

    public IEnumerable<string> Keys => _keyDefinition.Keys;

    public IEnumerable<object?> Values => _keyDefinition.Values.Select(v => PrimaryKey[v.Index]);

    public int Count => _keyDefinition.Count;

    public IEnumerable<KeyValuePair<string, object?>> Entries => 
        _keyDefinition.Select(e => new KeyValuePair<string, object?>(e.Key, PrimaryKey[e.Value.Index]));

    internal KeyRing(IServiceProvider serviceProvider, Dictionary<string, KeyDefinition> keyDefinition) =>
        (_serviceProvider, _keyDefinition) = (serviceProvider, keyDefinition);

    public IKeyRing Set(string name, object value)
    {
        this[name] = value;
        return this;
    }

    private object GetTarget(PropertyInfo[] path)
    {
        object? target = Source;
        foreach (PropertyInfo propertyInfo in path)
        {
            target = propertyInfo.GetValue(target, null);
            if (target is null)
            {
                target = _serviceProvider.GetRequiredService(propertyInfo.PropertyType);
            }
        }
        return target;
    }

}
