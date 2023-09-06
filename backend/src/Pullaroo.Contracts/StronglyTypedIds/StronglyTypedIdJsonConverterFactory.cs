using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pullaroo.Contracts.StronglyTypedIds;

public class StronglyTypedIdJsonConverterFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter?> _cache = new();

    public override bool CanConvert(Type typeToConvert) 
        => StronglyTypedIdHelper.IsStronglyTypedId(typeToConvert);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => _cache.GetOrAdd(typeToConvert, CreateConverter);

    private static JsonConverter? CreateConverter(Type typeToConvert)
    {
        if (!StronglyTypedIdHelper.IsStronglyTypedId(typeToConvert))
            throw new InvalidOperationException($"Cannot create converter for '{typeToConvert}'");

        var type = typeof(StronglyTypedIdJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(type);
    }
}
