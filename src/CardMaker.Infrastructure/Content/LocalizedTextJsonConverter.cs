using System.Text.Json;
using System.Text.Json.Serialization;
using CardMaker.Domain.Common;

namespace CardMaker.Infrastructure.Content;

/// <summary>Stesso formato JSON di <see cref="Persistence.LocalizedTextConverter"/>, ma per System.Text.Json.</summary>
internal sealed class LocalizedTextJsonConverter : JsonConverter<LocalizedText>
{
    public override LocalizedText? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
        return values is null ? new LocalizedText() : new LocalizedText(values);
    }

    public override void Write(Utf8JsonWriter writer, LocalizedText value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.Values, options);
}
