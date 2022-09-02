namespace HFM.Core.Services;

public sealed class NullFahProjectDetailsService : ProjectDetailsServiceBase
{
    public static NullFahProjectDetailsService Instance { get; } = new();

    /// <inheritdoc/>
    public override ProjectDetailsServiceGetResult TryGet(int projectID) => new() { Found = false, ProjectDetails = null };

    /// <inheritdoc/>
    public override async Task<ProjectDetailsServiceGetResult> TryGetWithRefreshAsync(int projectID) =>
        await Task.FromResult<ProjectDetailsServiceGetResult>(new() { Found = false, ProjectDetails = null }).ConfigureAwait(false);
}
