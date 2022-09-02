using HFM.Core.Logging;

namespace HFM.Core.Services;

public abstract class ProjectDetailsServiceBase
{
    public static ProjectDetailsServiceBase Default { get; } = new DefaultFahProjectDetailsService();

    protected ILogger Logger { get; set; }

    /// <summary>
    /// Sets the logger that the service will use to write log messages
    /// <br/>Should be set during service initialization at startup
    /// </summary>
    /// <param name="logger">The logger to register for this service</param>
    public void SetLogger(ILogger logger) => Logger = logger;

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
