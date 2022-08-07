using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using HFM.Core.Logging;
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

/// <summary>
/// Helper struct which can be implicitly cast as <see cref="bool"/> to get around
/// the fact that async methods cannot have out parameters to match TryDo method style
/// </summary>
public readonly struct ProjectDetailsServiceGetResult
{
    /// <summary>
    /// Flag indicates if the TryGet method found the desired record
    /// </summary>
    public bool Found { get; init; }

    /// <summary>
    /// The <see cref="FahProjectDetails"/> found that matched the project id passed to the TryGet method
    /// or null if no match was found.
    /// </summary>
    public FahProjectDetails ProjectDetails { get; init; }

    public bool ToBoolean() => Found;
    public static implicit operator bool(ProjectDetailsServiceGetResult details) => details.Found;
}

public abstract class ProjectDetailsService
{
    private static ProjectDetailsService _Default;
    // Initialize on first use
    public static ProjectDetailsService Default
    {
        get
        {
            if (_Default != null) return _Default;
            var service = new DefaultFahProjectDetailsService();
            // REVISIT: Currently the project list doesn't have a refresh of it's own
            service.InitializeProjectList();
            _Default = service;
            return _Default;
        }
    }

    public static ILogger Logger { get; set; }

    /// <summary>
    /// Attempts to get the project details for the project with the given <paramref name="projectID"/>
    /// from the project cache without executing cache miss or refresh logic if not present in the cache.
    /// </summary>
    /// <param name="projectID">The ID of the project details to return.</param>
    /// <returns>True with the project details if the <paramref name="projectID"/> exists in the cache; false and null otherwise. </returns>
    public abstract ProjectDetailsServiceGetResult TryGet(int projectID);

    /// <summary>
    /// Attempts to get the project details for the project with the given <paramref name="projectID"/>
    /// and executes cache miss logic if not present in the cache, or cache update logic if last check
    /// exceeds the last check timeout.
    /// </summary>
    /// <param name="projectID">The project ID of the protein to return.</param>
    /// <returns>True with the project details if the <paramref name="projectID"/> exists in the cache or could be added to the cache; false and null otherwise.</returns>
    public abstract Task<ProjectDetailsServiceGetResult> TryGetWithRefreshAsync(int projectID);
}

public class DefaultFahProjectDetailsService : ProjectDetailsService
{
    private readonly HttpClient _client = new();
    private readonly JsonSerializerOptions _jsonOptions = new();
    // REVISIT: Not sure if thread safety is a concern, but to be on the safe side using ConcurrentDictionary here
    //          This is not performance critical code so should be OK to be a little slower for the locking
    private readonly ConcurrentDictionary<int, FahProject> _projects = new();
    private readonly ConcurrentDictionary<int, FahProjectDetails> _projectDetails = new();

    public DefaultFahProjectDetailsService() : base()
    {
        _jsonOptions.Converters.Add(new FahApiDateTimeConverter());
    }

    /// <summary>
    /// This gets the list of all known projects from the API.
    /// These are used to map between individual projects and shared descriptions via description IDs
    /// </summary>
    internal void InitializeProjectList()
    {
        try
        {
            var projectList = _client.GetFromJsonAsync<FahProject[]>(FahUrl.ProjectBaseUrl, _jsonOptions).GetAwaiter().GetResult();
            foreach (var project in projectList)
            {
                _projects.TryAdd(project.ProjectId, project);
            }
        }
        catch (Exception e)
        {
            Logger?.Error($"Initialization of project details cache list failed with error: {e.Message}");
        }
    }

    /// <inheritdoc/>
    public override ProjectDetailsServiceGetResult TryGet(int projectID)
    {
        if (!_projects.TryGetValue(projectID, out var project))
            return new ProjectDetailsServiceGetResult { Found = false, ProjectDetails = null };

        return _projectDetails.TryGetValue(project.DescriptionId, out var projectDetails)
            ? new ProjectDetailsServiceGetResult { Found = true, ProjectDetails = projectDetails }
            : new ProjectDetailsServiceGetResult { Found = false, ProjectDetails = null };
    }

    /// <inheritdoc/>
    public override async Task<ProjectDetailsServiceGetResult> TryGetWithRefreshAsync(int projectID)
    {
        const double refreshHours = 24.0d;
        const double failedRefreshHours = 1.0d;
        var utcNow = DateTime.UtcNow;

        // The project needs to have been loaded for us to know what description ID to use for a lookup
        // Refresh of the projects collection happens elsewhere, this method only refreshes the project descriptions
        if (!_projects.TryGetValue(projectID, out var project))
            return new() { Found = false, ProjectDetails = null };

        int descriptionId = project.DescriptionId;
        bool found = _projectDetails.TryGetValue(descriptionId, out var projectDetails);
        bool needsRefresh = !found || utcNow.Subtract(projectDetails.LastSuccessfulRefresh).TotalHours < refreshHours;

        // If we found a value and it was last refreshed more recently than the refresh interval just return it
        if (!needsRefresh) return new() { Found = found, ProjectDetails = projectDetails };

        // Prevent querying too frequently for project details we recently failed to refresh
        if (projectDetails != null &&
            utcNow.Subtract(projectDetails.LastAttemptedRefresh).TotalHours < failedRefreshHours)
        {
            return new() { Found = found, ProjectDetails = projectDetails };
        }

        bool refreshSuccess = false;
        try
        {
            string uri = String.Format(CultureInfo.InvariantCulture, FahUrl.ProjectFindDescriptionApiUrlTemplate, descriptionId);
            var updatedDetails = await _client.GetFromJsonAsync<FahProjectDetails>(uri, _jsonOptions).ConfigureAwait(false);

            // Even if we got a response, if the associated projects list is null this is a failed refresh attempt
            if (!String.IsNullOrWhiteSpace(updatedDetails?.Projects))
            {
                // NOTE: We don't care if we updated or someone else updated because if two updates happened at the same time for the same ID
                //       they should produce different instances with the same values so it doesn't matter other than a wasted allocation
                projectDetails = _projectDetails.AddOrUpdate(descriptionId, updatedDetails, (key, oldValue) => updatedDetails);
                projectDetails.LastSuccessfulRefresh = utcNow;

                refreshSuccess = true;
                return new() { Found = true, ProjectDetails = projectDetails };
            }
        }
        catch (Exception e)
        {
            Logger?.Error($"Project {projectID} cache refresh failed with error: {e.Message}");
        }
        finally
        {
            // Regardless if refresh was successful or not, we need to set the last attempt flag to prevent
            // firing off API calls too frequently, especially after failed attempts
            if (projectDetails != null)
            {
                projectDetails.LastAttemptedRefresh = utcNow;
            }
            else
            {
                // We need to add an empty record against the description id because
                // the LastAttemptedRefresh value needs to be there to prevent over-calling the API for something missing
                _projectDetails.TryAdd(descriptionId, new FahProjectDetails
                {
                    LastSuccessfulRefresh = utcNow.AddYears(-1),
                    LastAttemptedRefresh = utcNow,
                });
            }
        }

        // Even if we were unable to successfully perform a refresh, we still should return the last cached value
        return new() { Found = found || refreshSuccess, ProjectDetails = projectDetails };
    }
}

public class NullFahProjectDetailsService : ProjectDetailsService
{
    public static NullFahProjectDetailsService Instance { get; } = new();

    /// <inheritdoc/>
    public override ProjectDetailsServiceGetResult TryGet(int projectID) => new() { Found = false, ProjectDetails = null };

    /// <inheritdoc/>
    public override async Task<ProjectDetailsServiceGetResult> TryGetWithRefreshAsync(int projectID) =>
        await Task.FromResult<ProjectDetailsServiceGetResult>(new() { Found = false, ProjectDetails = null }).ConfigureAwait(false);
}
