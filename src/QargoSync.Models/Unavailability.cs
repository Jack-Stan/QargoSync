using System.Text.Json.Serialization;

namespace QargoSync.Models;

/// <summary>
/// Represents an unavailability period for a resource in Qargo
/// </summary>
public record Unavailability
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Input model for creating or updating unavailabilities
/// </summary>
public class UnavailabilityInput
{
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Available unavailability reason types from Qargo API
/// </summary>
public static class UnavailabilityReasons
{
    public const string DriverHoliday = "DRIVER_HOLIDAY";
    public const string DriverSickness = "DRIVER_SICKNESS";
    public const string VehicleMaintenance = "VEHICLE_MAINTENANCE";
    public const string TrafficDelay = "TRAFFIC_DELAY";
    public const string BreakdownDelay = "BREAKDOWN_DELAY";
    public const string Other = "OTHER";
}