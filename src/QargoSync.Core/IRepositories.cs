using QargoSync.Models;
using QargoSync.Models.Configuration;

namespace QargoSync.Core;

/// <summary>
/// Repository interface for Qargo Resource operations
/// Implements Repository pattern for clean separation of concerns
/// </summary>
public interface IResourceRepository
{
    /// <summary>
    /// Get all resources from the specified environment
    /// </summary>
    Task<IEnumerable<Resource>> GetResourcesAsync();

    /// <summary>
    /// Get a specific resource by ID
    /// </summary>
    Task<Resource?> GetResourceAsync(string resourceId);

    /// <summary>
    /// Get all unavailabilities for a specific resource
    /// </summary>
    Task<IEnumerable<Unavailability>> GetUnavailabilitiesAsync(string resourceId);

    /// <summary>
    /// Get unavailabilities for a resource within a date range
    /// </summary>
    Task<IEnumerable<Unavailability>> GetUnavailabilitiesAsync(string resourceId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Get a specific unavailability by resource ID and unavailability ID
    /// </summary>
    Task<Unavailability?> GetUnavailabilityAsync(string resourceId, string unavailabilityId);

    /// <summary>
    /// Create a new unavailability for a resource
    /// </summary>
    Task<Unavailability> CreateUnavailabilityAsync(string resourceId, UnavailabilityInput unavailability);

    /// <summary>
    /// Update an existing unavailability
    /// </summary>
    Task<Unavailability> UpdateUnavailabilityAsync(string resourceId, string unavailabilityId, UnavailabilityInput unavailability);

    /// <summary>
    /// Delete an unavailability
    /// </summary>
    Task<bool> DeleteUnavailabilityAsync(string resourceId, string unavailabilityId);
}

/// <summary>
/// Repository interface for synchronization operations
/// Coordinates operations between master and target environments
/// </summary>
public interface ISyncRepository
{
    /// <summary>
    /// Synchronize unavailabilities from master to target environment
    /// </summary>
    Task<SyncResult> SynchronizeUnavailabilitiesAsync(SynchronizationSettings settings);

    /// <summary>
    /// Synchronize unavailabilities for a specific resource
    /// </summary>
    Task<ResourceSyncOperation> SynchronizeResourceUnavailabilitiesAsync(string resourceId, SynchronizationSettings settings);

    /// <summary>
    /// Compare unavailabilities between master and target environments
    /// </summary>
    Task<ResourceSyncOperation> CompareResourceUnavailabilitiesAsync(string resourceId);
}