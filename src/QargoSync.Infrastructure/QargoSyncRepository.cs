using Microsoft.Extensions.Logging;
using QargoSync.Core;
using QargoSync.Models;
using QargoSync.Models.Configuration;
using System.Diagnostics;

namespace QargoSync.Infrastructure;

/// <summary>
/// Repository implementation for synchronization operations
/// Coordinates data sync between master and target environments
/// </summary>
public class QargoSyncRepository : ISyncRepository
{
    private readonly IQargoHttpService _httpService;
    private readonly ILogger<QargoSyncRepository> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly QargoEnvironment _masterEnvironment;
    private readonly QargoEnvironment _targetEnvironment;

    public QargoSyncRepository(
        IQargoHttpService httpService,
        ILogger<QargoSyncRepository> logger,
        ILoggerFactory loggerFactory,
        QargoEnvironment masterEnvironment,
        QargoEnvironment targetEnvironment)
    {
        _httpService = httpService;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _masterEnvironment = masterEnvironment;
        _targetEnvironment = targetEnvironment;
    }

    public async Task<SyncResult> SynchronizeUnavailabilitiesAsync(SynchronizationSettings settings)
    {
        var stopwatch = Stopwatch.StartNew();
        var syncResult = new SyncResult();
        
        _logger.LogInformation("Starting unavailability synchronization from {Master} to {Target}", 
            _masterEnvironment.BaseUrl, _targetEnvironment.BaseUrl);

        try
        {
            // Get repositories for both environments
            var masterRepo = new QargoResourceRepository(_httpService, 
                _loggerFactory.CreateLogger<QargoResourceRepository>(), _masterEnvironment);
            var targetRepo = new QargoResourceRepository(_httpService, 
                _loggerFactory.CreateLogger<QargoResourceRepository>(), _targetEnvironment);

            // Get all resources from master environment
            var masterResources = await masterRepo.GetResourcesAsync();
            var filteredResources = masterResources.Where(r => r.Active).ToList();
            
            _logger.LogInformation("Found {TotalResources} resources in master environment, {ActiveResources} are active", 
                masterResources.Count(), filteredResources.Count);

            foreach (var resource in filteredResources)
            {
                try
                {
                    var resourceSync = await SynchronizeResourceUnavailabilitiesAsync(resource.Id, settings);
                    
                    syncResult.ResourcesProcessed++;
                    syncResult.UnavailabilitiesCreated += resourceSync.ToCreate.Count;
                    syncResult.UnavailabilitiesUpdated += resourceSync.ToUpdate.Count;
                    syncResult.UnavailabilitiesDeleted += resourceSync.ToDelete.Count;
                    
                    if (resourceSync.Errors.Any())
                    {
                        syncResult.Errors.AddRange(resourceSync.Errors);
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Failed to sync resource {resource.Id} ({resource.Name}): {ex.Message}";
                    _logger.LogError(ex, "Sync failed for resource {ResourceId}", resource.Id);
                    syncResult.Errors.Add(error);
                }
            }

            syncResult.Success = syncResult.Errors.Count == 0;
            
            _logger.LogInformation("Synchronization completed. Success: {Success}, " +
                "Resources: {Resources}, Created: {Created}, Updated: {Updated}, Deleted: {Deleted}, " +
                "Errors: {Errors}", 
                syncResult.Success, syncResult.ResourcesProcessed, 
                syncResult.UnavailabilitiesCreated, syncResult.UnavailabilitiesUpdated, 
                syncResult.UnavailabilitiesDeleted, syncResult.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Synchronization failed with unexpected error");
            syncResult.Success = false;
            syncResult.Errors.Add($"Synchronization failed: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            syncResult.Duration = stopwatch.Elapsed;
        }

        return syncResult;
    }

    public async Task<ResourceSyncOperation> SynchronizeResourceUnavailabilitiesAsync(string resourceId, SynchronizationSettings settings)
    {
        var operation = new ResourceSyncOperation { ResourceId = resourceId };
        
        try
        {
            // Get repositories for both environments
            var masterRepo = new QargoResourceRepository(_httpService, _loggerFactory.CreateLogger<QargoResourceRepository>(), _masterEnvironment);
            var targetRepo = new QargoResourceRepository(_httpService, _loggerFactory.CreateLogger<QargoResourceRepository>(), _targetEnvironment);

            // Get resource details
            var resource = await masterRepo.GetResourceAsync(resourceId);
            if (resource == null)
            {
                operation.Errors.Add($"Resource {resourceId} not found in master environment");
                return operation;
            }
            
            operation.ResourceName = resource.Name;

            // Get unavailabilities from master environment (filtered by 2025 date range)
            var masterUnavailabilities = await masterRepo.GetUnavailabilitiesAsync(
                resourceId, 
                settings.StartDate, 
                settings.EndDate);
            
            operation.MasterUnavailabilities = masterUnavailabilities.ToList();

            // Get existing unavailabilities from target environment  
            var targetUnavailabilities = await targetRepo.GetUnavailabilitiesAsync(
                resourceId,
                settings.StartDate,
                settings.EndDate);
            
            operation.TargetUnavailabilities = targetUnavailabilities.ToList();

            // Compare and determine what needs to be synchronized
            await DetermineChangesAsync(operation);

            // Execute the synchronization operations
            await ExecuteSyncOperationsAsync(targetRepo, operation);

            _logger.LogInformation("Synchronized resource {ResourceId}: Created {Created}, Updated {Updated}, Deleted {Deleted}", 
                resourceId, operation.ToCreate.Count, operation.ToUpdate.Count, operation.ToDelete.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synchronize resource {ResourceId}", resourceId);
            operation.Errors.Add($"Sync failed: {ex.Message}");
        }

        return operation;
    }

    public async Task<ResourceSyncOperation> CompareResourceUnavailabilitiesAsync(string resourceId)
    {
        var operation = new ResourceSyncOperation { ResourceId = resourceId };
        
        try
        {
            var masterRepo = new QargoResourceRepository(_httpService, _loggerFactory.CreateLogger<QargoResourceRepository>(), _masterEnvironment);
            var targetRepo = new QargoResourceRepository(_httpService, _loggerFactory.CreateLogger<QargoResourceRepository>(), _targetEnvironment);

            // Get resource details
            var resource = await masterRepo.GetResourceAsync(resourceId);
            if (resource == null)
            {
                operation.Errors.Add($"Resource {resourceId} not found in master environment");
                return operation;
            }
            
            operation.ResourceName = resource.Name;

            // Get unavailabilities from both environments for 2025
            var start2025 = new DateTime(2025, 1, 1);
            var end2025 = new DateTime(2025, 12, 31, 23, 59, 59);
            
            operation.MasterUnavailabilities = (await masterRepo.GetUnavailabilitiesAsync(resourceId, start2025, end2025)).ToList();
            operation.TargetUnavailabilities = (await targetRepo.GetUnavailabilitiesAsync(resourceId, start2025, end2025)).ToList();

            // Determine what changes would be made (without executing them)
            await DetermineChangesAsync(operation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare resource {ResourceId}", resourceId);
            operation.Errors.Add($"Comparison failed: {ex.Message}");
        }

        return operation;
    }

    private async Task DetermineChangesAsync(ResourceSyncOperation operation)
    {
        // Find unavailabilities that need to be created (exist in master but not target)
        foreach (var masterUnavailability in operation.MasterUnavailabilities)
        {
            var matchingTarget = operation.TargetUnavailabilities
                .FirstOrDefault(t => IsSameUnavailability(masterUnavailability, t));

            if (matchingTarget == null)
            {
                // Not found in target - needs to be created
                operation.ToCreate.Add(masterUnavailability);
            }
            else if (HasChanges(masterUnavailability, matchingTarget))
            {
                // Found but different - needs to be updated
                var updatedUnavailability = masterUnavailability with { Id = matchingTarget.Id };
                operation.ToUpdate.Add(updatedUnavailability);
            }
        }

        // Find unavailabilities that need to be deleted (exist in target but not master)
        foreach (var targetUnavailability in operation.TargetUnavailabilities)
        {
            var matchingMaster = operation.MasterUnavailabilities
                .FirstOrDefault(m => IsSameUnavailability(m, targetUnavailability));

            if (matchingMaster == null && !string.IsNullOrEmpty(targetUnavailability.Id))
            {
                // Not found in master - needs to be deleted
                operation.ToDelete.Add(targetUnavailability.Id);
            }
        }

        await Task.CompletedTask; // This method is currently synchronous but marked async for future expansion
    }

    private async Task ExecuteSyncOperationsAsync(QargoResourceRepository targetRepo, ResourceSyncOperation operation)
    {
        // Create new unavailabilities
        foreach (var unavailability in operation.ToCreate)
        {
            try
            {
                var input = new UnavailabilityInput
                {
                    ExternalId = unavailability.ExternalId,
                    StartTime = unavailability.StartTime,
                    EndTime = unavailability.EndTime,
                    Reason = unavailability.Reason,
                    Description = unavailability.Description
                };

                await targetRepo.CreateUnavailabilityAsync(operation.ResourceId, input);
            }
            catch (Exception ex)
            {
                operation.Errors.Add($"Failed to create unavailability: {ex.Message}");
            }
        }

        // Update existing unavailabilities
        foreach (var unavailability in operation.ToUpdate)
        {
            try
            {
                if (string.IsNullOrEmpty(unavailability.Id))
                {
                    operation.Errors.Add("Cannot update unavailability without ID");
                    continue;
                }

                var input = new UnavailabilityInput
                {
                    ExternalId = unavailability.ExternalId,
                    StartTime = unavailability.StartTime,
                    EndTime = unavailability.EndTime,
                    Reason = unavailability.Reason,
                    Description = unavailability.Description
                };

                await targetRepo.UpdateUnavailabilityAsync(operation.ResourceId, unavailability.Id, input);
            }
            catch (Exception ex)
            {
                operation.Errors.Add($"Failed to update unavailability {unavailability.Id}: {ex.Message}");
            }
        }

        // Delete removed unavailabilities
        foreach (var unavailabilityId in operation.ToDelete)
        {
            try
            {
                await targetRepo.DeleteUnavailabilityAsync(operation.ResourceId, unavailabilityId);
            }
            catch (Exception ex)
            {
                operation.Errors.Add($"Failed to delete unavailability {unavailabilityId}: {ex.Message}");
            }
        }
    }

    private static bool IsSameUnavailability(Unavailability master, Unavailability target)
    {
        // Compare by external_id if available, otherwise by time period and reason
        if (!string.IsNullOrEmpty(master.ExternalId) && !string.IsNullOrEmpty(target.ExternalId))
        {
            return master.ExternalId == target.ExternalId;
        }

        // Fallback to comparing by date/time and reason
        return master.StartTime == target.StartTime &&
               master.EndTime == target.EndTime &&
               master.Reason == target.Reason;
    }

    private static bool HasChanges(Unavailability master, Unavailability target)
    {
        return master.StartTime != target.StartTime ||
               master.EndTime != target.EndTime ||
               master.Reason != target.Reason ||
               master.Description != target.Description;
    }
}