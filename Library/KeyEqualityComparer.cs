using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Net.Leksi.KeyBox;
/// <summary>
/// <para xml:lang="ru">
/// Класс для выявления равенства ключей
/// </para>
/// <para xml:lang="en">
/// Class for detecting equality of keys
/// </para>
/// </summary>
public class KeyEqualityComparer : IEqualityComparer<object?[]?>
{
    /// <inheritdoc/>
    public bool Equals(object?[]? x, object?[]? y)
    {
        if (x == y)
        {
            return true;
        }
        if (x == null || y == null)
        {
            return false;
        }
        return x.Length == y.Length && x.Zip(y).All(v => v.First is { } && v.Second is { } && v.First.Equals(v.Second));
    }

    /// <inheritdoc/>
    public int GetHashCode(object?[]? obj)
    {
        return obj?.Select(v => v is null ? 0 : v.GetHashCode()).Aggregate(0, (v, res) => unchecked(v + res * 7)) ?? 42;
    }
}