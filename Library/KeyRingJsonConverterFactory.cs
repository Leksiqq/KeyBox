using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Net.Leksi.KeyBox;

public class KeyRingJsonConverterFactory : JsonConverterFactory
{
    public event PrimaryKeyEventHandler? PrimaryKeyFound
    {
        add
        {
            if(_primaryKeyEventHandler != null)
            {
                _primaryKeyEventHandler = null;
            }
            _primaryKeyEventHandler += value;
        }
        remove
        {
            _primaryKeyEventHandler -= value;
        }
    }

    public const string KeyPropertyName = "$key";
    public const string KeyOnlyPropertyName = "$keyOnly";

    private PrimaryKeyEventHandler? _primaryKeyEventHandler = null;
    private readonly IServiceProvider _serviceProvider;

    public KeyRingJsonConverterFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public override bool CanConvert(Type typeToConvert)
    {
        return _serviceProvider.GetRequiredService<IKeyBox>().HasMappedPrimaryKeys(typeToConvert);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(KeyRingJsonConverter<>).MakeGenericType(new Type[] { typeToConvert });
        return (JsonConverter)Activator.CreateInstance(converterType, _serviceProvider, this)!;
    }

    internal void OnPrimaryKeyFound(PrimaryKeyEventArgs args)
    {
        _primaryKeyEventHandler?.Invoke(args);
    }
}
