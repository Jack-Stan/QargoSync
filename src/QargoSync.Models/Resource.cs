using System.Text.Json.Serialization;

namespace QargoSync.Models;

/// <summary>
/// Represents a resource in Qargo
/// </summary>
public class Resource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;
}

/// <summary>
/// API response wrapper for paginated resource lists
/// </summary>
public class ResourceListResponse
{
    [JsonPropertyName("items")]
    public List<Resource> Results { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("next_cursor")]
    public string? Next { get; set; }

    [JsonPropertyName("previous")]
    public string? Previous { get; set; }
}

/// <summary>
/// API response wrapper for paginated unavailability lists
/// </summary>
public class UnavailabilityListResponse
{
    [JsonPropertyName("results")]
    public List<Unavailability> Results { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("previous")]
    public string? Previous { get; set; }
}