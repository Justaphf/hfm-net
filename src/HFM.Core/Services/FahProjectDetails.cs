using System.Globalization;
using System.Text.Json.Serialization;

namespace HFM.Core.Services;

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

    // Excluded because we don't actually need it for the current implementation.
    // I left it commented out becuase I initially included it, but also because I see a near-future implementation where
    // the project description (and possibly icon) gets displayed in the historical analysis view (but is beyond the scope of this feature).
    //[JsonPropertyName("description")]
    //public string Description { get; init; }

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
