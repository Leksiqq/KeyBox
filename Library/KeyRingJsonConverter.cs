using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Net.Leksi.KeyBox;

internal class KeyRingJsonConverter<T> : JsonConverter<T> where T : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KeyRingJsonConverterFactory _factory;

    public KeyRingJsonConverter(IServiceProvider serviceProvider, KeyRingJsonConverterFactory factory) => 
        (_serviceProvider, _factory) = (serviceProvider, factory);

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.StartObject)
        {
            T item = _serviceProvider.GetRequiredService<T>();
            Type itemType = item.GetType();
            IKeyRing? keyRing = null;
            bool keyOnly = false;

            PrimaryKeyEventArgs? primaryKeyEventArgs = null;

            while (reader.Read())
            {
                if (reader.TokenType is JsonTokenType.EndObject)
                {
                    return item;
                }

                if (reader.TokenType is not JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }


                string? propertyName = reader.GetString();
                if (propertyName is null)
                {
                    throw new JsonException();
                }

                if(propertyName == KeyRingJsonConverterFactory.KeyPropertyName)
                {
                    if(!reader.Read())
                    {
                        throw new JsonException();
                    }
                    keyRing = _serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(item);
                    if(keyRing is null || reader.TokenType != JsonTokenType.StartArray)
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
                else if(propertyName == KeyRingJsonConverterFactory.KeyOnlyPropertyName && reader.GetBoolean())
                {
                    keyOnly = true;
                }
                else
                {
                    if(keyRing is { })
                    {
                        primaryKeyEventArgs = new()
                        {
                            IsReading = true,
                            KeyRing = keyRing,
                            Value = item,
                            Interrupt = keyOnly,
                            TypeToConvert = typeToConvert
                        };
                        _factory.OnPrimaryKeyFound(primaryKeyEventArgs);
                        if (!object.ReferenceEquals(primaryKeyEventArgs.Value, item))
                        {
                            item = (T)primaryKeyEventArgs.Value;
                        }
                    }
                    if (
                        (primaryKeyEventArgs is null || !primaryKeyEventArgs.Interrupt)
                        && itemType.GetProperty(propertyName) is PropertyInfo propertyInfo 
                        && propertyInfo.CanWrite
                    )
                    {
                        propertyInfo.SetValue(item, JsonSerializer.Deserialize(ref reader, propertyInfo.PropertyType, options));
                    } 
                    else
                    {
                        JsonSerializer.Deserialize<object>(ref reader, options);
                    }
                }
            }
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            PrimaryKeyEventArgs? primaryKeyEventArgs = null;
            writer.WriteStartObject();
            IKeyBox keyBox = _serviceProvider.GetRequiredService<IKeyBox>();
            IKeyRing? keyRing = keyBox.GetKeyRing(value);
            if(keyRing is { })
            {
                writer.WritePropertyName(KeyRingJsonConverterFactory.KeyPropertyName);
                writer.WriteStartObject();
                writer.WritePropertyName(KeyRingJsonConverterFactory.TypeName);
                writer.WriteNumberValue((keyBox as KeyBox)!.GetTypeId(typeof(T)));
                writer.WritePropertyName(KeyRingJsonConverterFactory.KeyFieldsName);
                writer.WriteStartArray();
                foreach (var item in keyRing.Values)
                {
                    JsonSerializer.Serialize(writer, item, item?.GetType() ?? typeof(object), options);
                }
                writer.WriteEndArray();
                primaryKeyEventArgs = new()
                {
                    IsReading = false,
                    KeyRing = keyRing,
                    Value = value,
                    Interrupt = false,
                    TypeToConvert = typeof(T)
                };
                _factory.OnPrimaryKeyFound(primaryKeyEventArgs);
                if (primaryKeyEventArgs is { } && primaryKeyEventArgs.Interrupt)
                {
                    writer.WritePropertyName(KeyRingJsonConverterFactory.KeyOnlyPropertyName);
                    writer.WriteBooleanValue(true);
                }
                writer.WriteEndObject();
            }
            if(primaryKeyEventArgs is null || !primaryKeyEventArgs.Interrupt)
            {
                foreach (PropertyInfo pi in typeof(T).GetProperties())
                {
                    writer.WritePropertyName(pi.Name);
                    JsonSerializer.Serialize(writer, pi.GetValue(value), pi.PropertyType, options);
                }
            }
            writer.WriteEndObject();
        }
    }
}
