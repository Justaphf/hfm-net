using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HFM.Core.Serializers;

// NOTE: This code is a modification of example code from Microsoft's documentation
//       https://docs.microsoft.com/en-us/dotnet/standard/datetime/system-text-json-support#using-datetimeoffsetparse-as-a-fallback-to-the-serializers-native-parsing
public class FahApiDateTimeConverter : JsonConverter<DateTime>
{
    public const string FAH_DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(DateTime));

        // The default (fast) parsing requires the string format to be compliant with the extended ISO 8601-1:2019 profile.
        // The FAH API does not appear to use this format for DateTime values so don't waste time trying it here
        //if (reader.TryGetDateTime(out DateTime value)) return value;

        string valueString = reader.GetString() ?? string.Empty;

        // Try to parse with the format it looks like FAH is using: "yyyy-MM-dd HH:mm:ss"
        if (DateTime.TryParseExact(valueString, FAH_DATETIME_FORMAT, null, System.Globalization.DateTimeStyles.None, out var value)) return value;

        // In case the format gets changed, or other API calls return a different format allow
        // fall back to the slower, broad format support parse method
        return DateTime.Parse(valueString);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var valString = value.ToString(FAH_DATETIME_FORMAT);
        writer.WriteStringValue(valString);
    }
}
