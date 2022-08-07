using System.Text.Json;

using NUnit.Framework;

namespace HFM.Core.Serializers;

[TestFixture]
public class FahApiDateTimeConverterTests
{
    // NOTE: When testing JSON string serialization/deserialization remmeber that the JsonSeraializer expects
    //       the values it will be parsing to be quoted and those quotes included in what it tries to deserialize
    private static readonly DateTime TestDateTime_Value1 = new(2021, 11, 23, 3, 24, 56);
    private const string TestDateTime_JsonString1 = "\"2021-11-23 03:24:56\"";

    private static readonly DateTime TestDateTime_Value2 = new(2021, 11, 11, 22, 24, 56);
    private const string TestDateTime_JsonString2 = "\"2021-11-11 22:24:56\"";

    [Test]
    public void TestDeserializeDateTime()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new FahApiDateTimeConverter());

        Assert.AreEqual(TestDateTime_Value1, JsonSerializer.Deserialize<DateTime>(TestDateTime_JsonString1, options));
        Assert.AreEqual(TestDateTime_Value2, JsonSerializer.Deserialize<DateTime>(TestDateTime_JsonString2, options));
    }

    [Test]
    public void TestSerializeDateTime()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new FahApiDateTimeConverter());

        Assert.AreEqual(TestDateTime_JsonString1, JsonSerializer.Serialize(TestDateTime_Value1, options));
        Assert.AreEqual(TestDateTime_JsonString2, JsonSerializer.Serialize(TestDateTime_Value2, options));
    }
}
