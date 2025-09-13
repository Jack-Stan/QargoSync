using Microsoft.Extensions.Logging;
using QargoSync.Core;
using QargoSync.Models;
using QargoSync.Models.Configuration;
using System.Text.Json;
using System.Text;

namespace QargoSync.Infrastructure;

/// <summary>
/// HTTP service implementation for Qargo API operations
/// Provides authenticated HTTP client instances and common operations
/// </summary>
public class QargoHttpService : IQargoHttpService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<QargoHttpService> _logger;

    public QargoHttpService(
        IHttpClientFactory httpClientFactory,
        IAuthenticationService authService,
        ILogger<QargoHttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _authService = authService;
        _logger = logger;
    }

    public async Task<HttpClient> GetAuthenticatedClientAsync(QargoEnvironment environment)
    {
        var client = _httpClientFactory.CreateClient("QargoApi");
        client.BaseAddress = new Uri(environment.BaseUrl);

        var token = await _authService.GetAccessTokenAsync(environment);
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "QargoSync/1.0");

        return client;
    }

    public async Task<T?> GetAsync<T>(QargoEnvironment environment, string endpoint)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync(environment);
            var url = endpoint.StartsWith('/') ? endpoint : $"/{endpoint}";
            
            _logger.LogDebug("GET {BaseUrl}{Endpoint}", environment.BaseUrl, url);
            
            var response = await client.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                await LogErrorResponse(response, $"GET {url}");
                return default;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, GetJsonOptions());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GET request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(QargoEnvironment environment, string endpoint, object? body = null)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync(environment);
            var url = endpoint.StartsWith('/') ? endpoint : $"/{endpoint}";
            
            StringContent? content = null;
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, GetJsonOptions());
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            _logger.LogDebug("POST {BaseUrl}{Endpoint}", environment.BaseUrl, url);
            
            var response = await client.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                await LogErrorResponse(response, $"POST {url}");
                return default;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseJson, GetJsonOptions());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during POST request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PutAsync<T>(QargoEnvironment environment, string endpoint, object body)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync(environment);
            var url = endpoint.StartsWith('/') ? endpoint : $"/{endpoint}";
            
            var json = JsonSerializer.Serialize(body, GetJsonOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("PUT {BaseUrl}{Endpoint}", environment.BaseUrl, url);
            
            var response = await client.PutAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                await LogErrorResponse(response, $"PUT {url}");
                return default;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseJson, GetJsonOptions());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PUT request to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(QargoEnvironment environment, string endpoint)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync(environment);
            var url = endpoint.StartsWith('/') ? endpoint : $"/{endpoint}";
            
            _logger.LogDebug("DELETE {BaseUrl}{Endpoint}", environment.BaseUrl, url);
            
            var response = await client.DeleteAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                await LogErrorResponse(response, $"DELETE {url}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DELETE request to {Endpoint}", endpoint);
            throw;
        }
    }

    private async Task LogErrorResponse(HttpResponseMessage response, string operation)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogError("API request failed: {Operation} returned {StatusCode}. Response: {ErrorContent}", 
            operation, response.StatusCode, errorContent);
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
}