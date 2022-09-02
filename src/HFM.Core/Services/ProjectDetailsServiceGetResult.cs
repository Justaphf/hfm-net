namespace HFM.Core.Services;

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
