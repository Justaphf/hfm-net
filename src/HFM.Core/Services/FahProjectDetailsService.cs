using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using HFM.Core.Serializers;

namespace HFM.Core.Services;

public sealed class FahProject
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

public sealed class FahProjectDetails
{
    private string _cause;

    [JsonPropertyName("cause")]
    public string Cause
    {
        get => _cause;
        // Convert to title case one time when we set this so we don't waste cycles every time we display in the UI
        init => _cause = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
    }

    [JsonPropertyName("description")]
    public string Description { get; init; }

    [JsonPropertyName("manager")]
    public string Manager { get; init; }

    [JsonPropertyName("modified")]
    public DateTime Modified { get; init; }

    [JsonPropertyName("projects")]
    public string Projects { get; init; }

    [JsonIgnore]
    public DateTime LastSuccessfulRefresh { get; set; }

    [JsonIgnore]
    public DateTime LastAttemptedRefresh { get; set; }
}
