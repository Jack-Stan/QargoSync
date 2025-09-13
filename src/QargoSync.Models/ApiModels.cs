using System.Text.Json.Serialization;

namespace QargoSync.Models;

/// <summary>
/// OAuth2 token request for Qargo API authentication
/// </summary>
public class AuthTokenRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = "client_credentials";

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>
/// OAuth2 token response from Qargo API
/// </summary>
public class AuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

/// <summary>
/// Generic API error response structure
/// </summary>
public class ApiErrorResponse
{
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}