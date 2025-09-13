using QargoSync.Models;
using QargoSync.Models.Configuration;

namespace QargoSync.Core;

/// <summary>
/// Service interface for Qargo API authentication
/// Handles OAuth2 token management with automatic refresh
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Get a valid access token for the specified environment
    /// </summary>
    Task<string> GetAccessTokenAsync(QargoEnvironment environment);

    /// <summary>
    /// Force refresh the access token for the specified environment
    /// </summary>
    Task<string> RefreshAccessTokenAsync(QargoEnvironment environment);

    /// <summary>
    /// Check if the current token is still valid
    /// </summary>
    bool IsTokenValid(QargoEnvironment environment);
}

/// <summary>
/// Service interface for HTTP operations with Qargo API
/// Provides configured HttpClient instances with authentication
/// </summary>
public interface IQargoHttpService
{
    /// <summary>
    /// Get an authenticated HttpClient for the specified environment
    /// </summary>
    Task<HttpClient> GetAuthenticatedClientAsync(QargoEnvironment environment);

    /// <summary>
    /// Make an authenticated GET request and deserialize the response
    /// </summary>
    Task<T?> GetAsync<T>(QargoEnvironment environment, string endpoint);

    /// <summary>
    /// Make an authenticated POST request with JSON body
    /// </summary>
    Task<T?> PostAsync<T>(QargoEnvironment environment, string endpoint, object? body = null);

    /// <summary>
    /// Make an authenticated PUT request with JSON body
    /// </summary>
    Task<T?> PutAsync<T>(QargoEnvironment environment, string endpoint, object body);

    /// <summary>
    /// Make an authenticated DELETE request
    /// </summary>
    Task<bool> DeleteAsync(QargoEnvironment environment, string endpoint);
}