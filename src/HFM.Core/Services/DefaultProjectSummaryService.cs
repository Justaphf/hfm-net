using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

using HFM.Core.Logging;
using HFM.Core.Serializers;

namespace HFM.Core.Services;

public class DefaultFahProjectSummaryService : ProjectSummaryServiceBase, IDisposable
{
    public const double MIN_REFRESH_INTERVAL_MINUTES = 1440.0d;
    private bool _isDisposed = false;
    private DateTime _lastRefresh_UTC = DateTime.MinValue;
    private readonly SemaphoreSlim _refreshSync = new(1, 1);
    private readonly HttpClient _client = new();
    private readonly JsonSerializerOptions _jsonOptions = new();
    // REVISIT: Not sure if thread safety is a concern, but to be on the safe side using ConcurrentDictionary here
    //          This is not performance critical code so should be OK to be a little slower for the locking
    private readonly ConcurrentDictionary<int, FahProjectSummary> _projects = new();

    public DefaultFahProjectSummaryService() : base()
    {
        _jsonOptions.Converters.Add(new FahApiDateTimeConverter());
    }

    /// <inheritdoc/>
    public override bool TryGet(int projectID, out FahProjectSummary project) => _projects.TryGetValue(projectID, out project);

    /// <inheritdoc/>
    public override async Task<bool> TryRefreshAsync(bool force)
    {
        // Only refresh once every 24-hours, unless the force option was specified
        var now_UTC = DateTime.UtcNow;
        if (!force
            && _lastRefresh_UTC != DateTime.MinValue
            && now_UTC.Subtract(_lastRefresh_UTC).TotalMinutes < MIN_REFRESH_INTERVAL_MINUTES)
            return true;

        try
        {
            // NOTE: The lock keyword does not work with await's so need to use a semaphore instead
            await _refreshSync.WaitAsync().ConfigureAwait(false);
            if (!force
                && _lastRefresh_UTC != DateTime.MinValue
                && now_UTC.Subtract(_lastRefresh_UTC).TotalMinutes < MIN_REFRESH_INTERVAL_MINUTES)
                return true;

            Logger?.Info("Attempting to update project summary list from FAH API.");
            var timer = Stopwatch.StartNew();
            // Locks need to be on the same thread
            var projectList = await _client.GetFromJsonAsync<FahProjectSummary[]>(FahUrl.ProjectBaseUrl, _jsonOptions).ConfigureAwait(false);
            foreach (var project in projectList)
            {
                var _ = _projects.AddOrUpdate(project.ProjectId, project, (key, oldValue) => project);
            }
            timer.Stop();
            Logger?.Info($"Project summary list successfully updated from FAH API in {timer.GetExecTime()}.");
            _lastRefresh_UTC = now_UTC;

            return true;
        }
        catch (Exception e)
        {
            Logger?.Error($"Update of project details cache list failed with error: {e.Message}");
        }
        finally
        {
            _refreshSync.Release();
        }
        return false;
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
