using System.Text.Json.Serialization;

namespace HFM.Core.Services;

/// <summary>
/// Model object for deserializing the results of querying the FAH service on the following URL
/// <br/>https://api2.foldingathome.org/project
/// <br/>Current as of 2022-09-01
/// </summary>
public sealed class FahProjectSummary
{
    [JsonPropertyName("id")]
    public int ProjectId { get; init; }

    [JsonPropertyName("description")]
    public int DescriptionId { get; init; }

    [JsonPropertyName("manager")]
    public string Manager { get; init; }

    [JsonPropertyName("modified")]
    public DateTime Modified { get; init; }
}
