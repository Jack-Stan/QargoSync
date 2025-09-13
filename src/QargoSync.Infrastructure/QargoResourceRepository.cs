using Microsoft.Extensions.Logging;
using QargoSync.Core;
using QargoSync.Models;
using QargoSync.Models.Configuration;

namespace QargoSync.Infrastructure;

/// <summary>
/// Repository implementation for Qargo Resource operations
/// Implements Repository pattern with clean API abstraction
/// </summary>
public class QargoResourceRepository : IResourceRepository
{
    private readonly IQargoHttpService _httpService;
    private readonly ILogger<QargoResourceRepository> _logger;
    private readonly QargoEnvironment _environment;

    public QargoResourceRepository(
        IQargoHttpService httpService,
        ILogger<QargoResourceRepository> logger,
        QargoEnvironment environment)
    {
        _httpService = httpService;
        _logger = logger;
        _environment = environment;
    }

    public async Task<IEnumerable<Resource>> GetResourcesAsync()
    {
        _logger.LogInformation("Fetching all resources from {Environment}", _environment.BaseUrl);
        
        var allResources = new List<Resource>();
        var url = "/v1/resources/resource";
        
        do
        {
            var response = await _httpService.GetAsync<ResourceListResponse>(_environment, url);
            
            if (response?.Results != null)
            {
                allResources.AddRange(response.Results);
                _logger.LogDebug("Fetched {Count} resources, total: {Total}", 
                    response.Results.Count, allResources.Count);
            }
            
            // Handle pagination - get next page URL
            url = response?.Next;
            
        } while (!string.IsNullOrEmpty(url));

        _logger.LogInformation("Successfully fetched {TotalCount} resources", allResources.Count);
        return allResources;
    }

    public async Task<Resource?> GetResourceAsync(string resourceId)
    {
        _logger.LogDebug("Fetching resource {ResourceId}", resourceId);
        
        var resource = await _httpService.GetAsync<Resource>(_environment, $"/v1/resources/resource/{resourceId}");
        
        if (resource != null)
        {
            _logger.LogDebug("Successfully fetched resource {ResourceId}: {ResourceName}", resourceId, resource.Name);
        }
        else
        {
            _logger.LogWarning("Resource {ResourceId} not found", resourceId);
        }

        return resource;
    }

    public async Task<IEnumerable<Unavailability>> GetUnavailabilitiesAsync(string resourceId)
    {
        return await GetUnavailabilitiesAsync(resourceId, null, null);
    }

    public async Task<IEnumerable<Unavailability>> GetUnavailabilitiesAsync(string resourceId, DateTime? startDate = null, DateTime? endDate = null)
    {
        _logger.LogInformation("Fetching unavailabilities for resource {ResourceId}", resourceId);
        
        var allUnavailabilities = new List<Unavailability>();
        var baseUrl = $"/v1/resources/resource/{resourceId}/unavailability";
        
        // Build query parameters for date filtering
        var queryParams = new List<string>();
        if (startDate.HasValue)
        {
            queryParams.Add($"start_time_gte={startDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }
        if (endDate.HasValue)
        {
            queryParams.Add($"end_time_lte={endDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }
        
        var url = baseUrl;
        if (queryParams.Any())
        {
            url += "?" + string.Join("&", queryParams);
        }

        do
        {
            var response = await _httpService.GetAsync<UnavailabilityListResponse>(_environment, url);
            
            if (response?.Results != null)
            {
                allUnavailabilities.AddRange(response.Results);
                _logger.LogDebug("Fetched {Count} unavailabilities for resource {ResourceId}, total: {Total}", 
                    response.Results.Count, resourceId, allUnavailabilities.Count);
            }
            
            // Handle pagination
            url = response?.Next;
            
        } while (!string.IsNullOrEmpty(url));

        _logger.LogInformation("Successfully fetched {TotalCount} unavailabilities for resource {ResourceId}", 
            allUnavailabilities.Count, resourceId);
        
        return allUnavailabilities;
    }

    public async Task<Unavailability?> GetUnavailabilityAsync(string resourceId, string unavailabilityId)
    {
        _logger.LogDebug("Fetching unavailability {UnavailabilityId} for resource {ResourceId}", 
            unavailabilityId, resourceId);
        
        var unavailability = await _httpService.GetAsync<Unavailability>(_environment, 
            $"/v1/resources/resource/{resourceId}/unavailability/{unavailabilityId}");
        
        if (unavailability != null)
        {
            _logger.LogDebug("Successfully fetched unavailability {UnavailabilityId}", unavailabilityId);
        }
        else
        {
            _logger.LogWarning("Unavailability {UnavailabilityId} not found for resource {ResourceId}", 
                unavailabilityId, resourceId);
        }

        return unavailability;
    }

    public async Task<Unavailability> CreateUnavailabilityAsync(string resourceId, UnavailabilityInput unavailability)
    {
        _logger.LogInformation("Creating unavailability for resource {ResourceId}: {StartTime} to {EndTime}", 
            resourceId, unavailability.StartTime, unavailability.EndTime);
        
        var result = await _httpService.PostAsync<Unavailability>(_environment, 
            $"/v1/resources/resource/{resourceId}/unavailability", unavailability);
        
        if (result != null)
        {
            _logger.LogInformation("Successfully created unavailability {UnavailabilityId} for resource {ResourceId}", 
                result.Id, resourceId);
        }
        else
        {
            throw new InvalidOperationException($"Failed to create unavailability for resource {resourceId}");
        }

        return result;
    }

    public async Task<Unavailability> UpdateUnavailabilityAsync(string resourceId, string unavailabilityId, UnavailabilityInput unavailability)
    {
        _logger.LogInformation("Updating unavailability {UnavailabilityId} for resource {ResourceId}", 
            unavailabilityId, resourceId);
        
        var result = await _httpService.PutAsync<Unavailability>(_environment, 
            $"/v1/resources/resource/{resourceId}/unavailability/{unavailabilityId}", unavailability);
        
        if (result != null)
        {
            _logger.LogInformation("Successfully updated unavailability {UnavailabilityId}", unavailabilityId);
        }
        else
        {
            throw new InvalidOperationException($"Failed to update unavailability {unavailabilityId} for resource {resourceId}");
        }

        return result;
    }

    public async Task<bool> DeleteUnavailabilityAsync(string resourceId, string unavailabilityId)
    {
        _logger.LogInformation("Deleting unavailability {UnavailabilityId} for resource {ResourceId}", 
            unavailabilityId, resourceId);
        
        var success = await _httpService.DeleteAsync(_environment, 
            $"/v1/resources/resource/{resourceId}/unavailability/{unavailabilityId}");
        
        if (success)
        {
            _logger.LogInformation("Successfully deleted unavailability {UnavailabilityId}", unavailabilityId);
        }
        else
        {
            _logger.LogWarning("Failed to delete unavailability {UnavailabilityId} for resource {ResourceId}", 
                unavailabilityId, resourceId);
        }

        return success;
    }
}