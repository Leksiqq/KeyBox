using System.Reflection;

namespace Net.Leksi.KeyBox;

internal class KeyDefinition
{
    internal int Index { get; set; } = -1;
    internal Type? Type { get; set; } = null;
    internal PropertyInfo[]? Path { get; set; } = null;
    internal string? KeyFieldName { get; set; } = null;
}
