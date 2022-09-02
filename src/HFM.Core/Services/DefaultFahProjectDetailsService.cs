using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

using HFM.Core.Logging;
using HFM.Core.Serializers;

namespace HFM.Core.Services;

public class DefaultFahProjectDetailsService : ProjectDetailsServiceBase, IDisposable
{
    private bool _isDisposed = false;
    private readonly HttpClient _client = new();
    private readonly JsonSerializerOptions _jsonOptions = new();
    // REVISIT: Not sure if thread safety is a concern, but to be on the safe side using ConcurrentDictionary here
    //          This is not performance critical code so should be OK to be a little slower for the locking
    private readonly ConcurrentDictionary<int, FahProjectDetails> _projectDetails = new();

    public DefaultFahProjectDetailsService() : base()
    {
        _jsonOptions.Converters.Add(new FahApiDateTimeConverter());
    }

    /// <inheritdoc/>
    public override ProjectDetailsServiceGetResult TryGet(int projectID)
    {
        if (!ProjectSummaryServiceBase.Default.TryGet(projectID, out var project))
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

        // The project needs to have been loaded for us to know what description ID to use for a lookup
        // NOTE: This call will only actually refresh if needed based on refresh limits
        await ProjectSummaryServiceBase.Default.TryRefreshAsync(false).ConfigureAwait(false);

        var utcNow = DateTime.UtcNow;
        if (!ProjectSummaryServiceBase.Default.TryGet(projectID, out var project))
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
            Logger?.Info($"Attempting to update project cause from FAH API for project: {projectID}");
            var timer = Stopwatch.StartNew();
            string uri = String.Format(CultureInfo.InvariantCulture, FahUrl.ProjectFindDescriptionApiUrlTemplate, descriptionId);
            var updatedDetails = await _client.GetFromJsonAsync<FahProjectDetails>(uri, _jsonOptions).ConfigureAwait(false);

            // Even if we got a response, if the associated projects list is null this is a failed refresh attempt
            if (!String.IsNullOrWhiteSpace(updatedDetails?.Projects))
            {
                // NOTE: We don't care if we updated or someone else updated because if two updates happened at the same time for the same ID
                //       they should produce different instances with the same values so it doesn't matter other than a wasted allocation
                projectDetails = _projectDetails.AddOrUpdate(descriptionId, updatedDetails, (key, oldValue) => updatedDetails);
                projectDetails.LastSuccessfulRefresh = utcNow;
                timer.Stop();
                Logger?.Info($"Project {projectID} details successfully updated from FAH API in {timer.GetExecTime()}.");

                refreshSuccess = true;
                return new() { Found = true, ProjectDetails = projectDetails };
            }
            Logger?.Warn($"Project {projectID} details not found in FAH API result.");
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

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _client.Dispose();
        }

        _isDisposed = true;
    }
}
