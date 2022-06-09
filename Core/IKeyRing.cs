namespace Net.Leksi.KeyBox;

public interface IKeyRing
{
    object? this[string name] { get; set; }
    IEnumerable<string> Keys { get; }
    IEnumerable<object?> Values { get; }
    IEnumerable<KeyValuePair<string, object?>> Entries { get; }
    bool IsCompleted { get; }
    int Count { get; }
    object Source { get; }
    IKeyRing Set(string name, object value);
}
