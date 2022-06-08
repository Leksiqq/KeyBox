namespace Net.Leksi.KeyBox;

public class KeyRing : IKeyRing
{
    private readonly Dictionary<string, KeyDefinition> _keyDefinition;

    internal object[] PrimaryKey { get; set; } = null!;
    
    public object Source { get; internal set; } = null!;

    public object? this[string name] 
    { 
        get 
        {
            return PrimaryKey[_keyDefinition[name].Index];
        } 
        set 
        { 
            if((PrimaryKey[_keyDefinition[name].Index] is null) && (value is { }))
            {
                PrimaryKey[_keyDefinition[name].Index] = value;
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

    public IEnumerable<object> Values => _keyDefinition.Values.Select(v => PrimaryKey[v.Index]);

    public int Count => _keyDefinition.Count;

    public IEnumerable<KeyValuePair<string, object?>> Entries => 
        _keyDefinition.Select(e => new KeyValuePair<string, object?>(e.Key, PrimaryKey[e.Value.Index]));

    internal KeyRing(Dictionary<string, KeyDefinition> keyDefinition) => 
        _keyDefinition = keyDefinition;

    public void Clear()
    {
        if(PrimaryKey is { })
        {
            Array.Clear(PrimaryKey);
        }
    }

    public IKeyRing Set(string name, object value)
    {
        this[name] = value;
        return this;
    }

}
