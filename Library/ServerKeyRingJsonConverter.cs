using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Net.Leksi.KeyBox
{
    public class ServerKeyRingJsonConverter : JsonConverter<object>
    {
        public const string KeyPropertyName = "$key";
        public const string KeyFieldsName = "$";
        public const string TypeName = "$type";
        public const string KeyOnlyPropertyName = "$keyOnly";

        private readonly IServiceProvider _serviceProvider;
        private readonly IKeyBox _keyBox;

        public bool CachedObjectFound { get; private set; }
        public bool PrimaryKeyFound { get; private set; }

        public ServerKeyRingJsonConverter(IServiceProvider serviceProvider) =>
            (_serviceProvider, _keyBox) = (serviceProvider, _serviceProvider.GetRequiredService<IKeyBox>());

        public static ServerKeyRingJsonConverter? GetInstance(JsonSerializerOptions options)
        {
            return (ServerKeyRingJsonConverter?)options.Converters.Where(item => item is ServerKeyRingJsonConverter).FirstOrDefault();
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return false;
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            object? source = null;
            if (!reader.Read() || reader.TokenType is not JsonTokenType.StartObject)
            {
                throw new JsonException();
            }
            IKeyRing? keyRing = _keyBox.GetKeyRing(typeToConvert);
            while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
            {
                string? propertyName = reader.GetString();
                if (propertyName == KeyFieldsName)
                {
                    if (keyRing is null || reader.TokenType != JsonTokenType.StartArray)
                    {
                        JsonSerializer.Deserialize<object>(ref reader, options);
                    }
                    else
                    {
                        for (int i = 0; reader.Read() && reader.TokenType is not JsonTokenType.EndArray; ++i)
                        {
                            keyRing[i] = JsonSerializer.Deserialize(ref reader, keyRing.GetPartType(i), options);
                        }
                    }
                }
                else
                {
                    JsonSerializer.Deserialize<object>(ref reader, options);
                }
            }
            CachedObjectFound = false;
            PrimaryKeyFound = keyRing is { };
            if (PrimaryKeyFound)
            {

            }
            return source;
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            IKeyRing? keyRing = _keyBox.GetKeyRing(value);
            PrimaryKeyFound = keyRing is { } && keyRing.IsCompleted;
            if (PrimaryKeyFound)
            {
                writer.WritePropertyName(KeyPropertyName);
                writer.WriteStartObject();
                writer.WritePropertyName(TypeName);
                writer.WriteNumberValue((_keyBox as KeyBox)!.GetTypeId(value.GetType()));
                writer.WritePropertyName(KeyFieldsName);
                writer.WriteStartArray();
                foreach (var item in keyRing!.Values)
                {
                    JsonSerializer.Serialize(writer, item, item?.GetType() ?? typeof(object), options);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                CachedObjectFound = false;
            }
        }
    }
}
