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
public class KeyEqualityComparer : IEqualityComparer<IEnumerable<object>>
{
    /// <inheritdoc/>
    public bool Equals(IEnumerable<object>? x, IEnumerable<object>? y)
    {
        if (x == y)
        {
            return true;
        }
        if (x == null || y == null)
        {
            return false;
        }
        return x.Count() == y.Count() && x.Zip(y).All(v => v.First is { } && v.Second is { } && v.First.Equals(v.Second));
    }

    /// <inheritdoc/>
    public int GetHashCode(IEnumerable<object> obj)
    {
        int result = obj.Select(v => v is null ? 0 : v.GetHashCode()).Aggregate(0, (v, res) => unchecked(v + res * 7));
        return result;
    }
}