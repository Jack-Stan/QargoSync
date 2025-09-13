namespace QargoSync.Models;

/// <summary>
/// Represents the result of a synchronization operation
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public int ResourcesProcessed { get; set; }
    public int UnavailabilitiesCreated { get; set; }
    public int UnavailabilitiesUpdated { get; set; }
    public int UnavailabilitiesDeleted { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public DateTime SyncTimestamp { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"Sync {(Success ? "completed" : "failed")} in {Duration.TotalSeconds:F2}s. " +
               $"Processed {ResourcesProcessed} resources, " +
               $"Created {UnavailabilitiesCreated}, " +
               $"Updated {UnavailabilitiesUpdated}, " +
               $"Deleted {UnavailabilitiesDeleted} unavailabilities. " +
               $"Errors: {Errors.Count}, Warnings: {Warnings.Count}";
    }
}

/// <summary>
/// Represents a single synchronization operation for a resource
/// </summary>
public class ResourceSyncOperation
{
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public List<Unavailability> MasterUnavailabilities { get; set; } = new();
    public List<Unavailability> TargetUnavailabilities { get; set; } = new();
    public List<Unavailability> ToCreate { get; set; } = new();
    public List<Unavailability> ToUpdate { get; set; } = new();
    public List<string> ToDelete { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}