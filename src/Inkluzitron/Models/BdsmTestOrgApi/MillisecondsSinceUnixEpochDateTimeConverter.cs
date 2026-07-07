using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Inkluzitron.Models.BdsmTestOrgApi;

internal class MillisecondsSinceUnixEpochDateTimeConverter : DateTimeConverterBase
{
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.Integer)
            throw new JsonSerializationException($"Cannot read from non-integer: {reader.TokenType}");

        var millisecondsSinceUnixEpoch = (long)reader.Value;
        return DateTime.UnixEpoch.AddMilliseconds(millisecondsSinceUnixEpoch);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is DateTime { } dt)
            writer.WriteValue((long)dt.Subtract(DateTime.UnixEpoch).TotalSeconds);
        else if (value is DateTimeOffset { } dto)
            writer.WriteValue((long)dto.Subtract(DateTimeOffset.UnixEpoch).TotalSeconds);
        else
            throw new JsonSerializationException("Cannot write from anything but DateTime or DateTimeOffset");
    }
}
