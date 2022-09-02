using HFM.Core.Logging;

namespace HFM.Core.Services;
public abstract class ProjectSummaryServiceBase
{
    public static ProjectSummaryServiceBase Default { get; } = new DefaultFahProjectSummaryService();

    protected ILogger Logger { get; set; }

    /// <summary>
    /// Sets the logger that the service will use to write log messages
    /// <br/>Should be set during service initialization at startup
    /// </summary>
    /// <param name="logger">The logger to register for this service</param>
    public void SetLogger(ILogger logger) => Logger = logger;

    /// <summary>
    /// Attempts to get the project summary for the project ID with the given <paramref name="projectID"/>
    /// from the project cache without executing cache miss or refresh logic if not present in the cache.
    /// </summary>
    /// <param name="projectID">The ID of the project summary to return.</param>
    /// <returns>True with the project summary if the <paramref name="projectID"/> exists in the cache; false and null otherwise. </returns>
    public abstract bool TryGet(int projectID, out FahProjectSummary project);

    /// <summary>
    /// Attempts to get the project summary list for all projects and update the in-memory cache
    /// <br/>By default will only actually refresh if the last refresh was more than 24-hours ago.
    /// </summary>
    /// <param name="force">Forces the refresh, ignoring the last successful refresh timestamp.</param>
    /// <returns>True if the project summary cache could be updated from the API; false otherwise.</returns>
    public abstract Task<bool> TryRefreshAsync(bool force);
}
