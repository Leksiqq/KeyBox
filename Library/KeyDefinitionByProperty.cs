using System.Reflection;

namespace Net.Leksi.KeyBox;

internal class KeyDefinitionByProperty: KeyDefinition
{
    internal PropertyInfo[]? PropertiesPath { get; set; } = null;
}
