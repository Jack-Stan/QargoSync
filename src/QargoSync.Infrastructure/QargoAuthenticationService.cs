using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QargoSync.Core;
using QargoSync.Models;
using QargoSync.Models.Configuration;
using System.Text.Json;
using System.Text;

namespace QargoSync.Infrastructure;

/// <summary>
/// Implementation of OAuth2 authentication for Qargo API
/// Handles token caching and automatic refresh
/// </summary>
public class QargoAuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<QargoAuthenticationService> _logger;
    private readonly QargoSettings _settings;

    public QargoAuthenticationService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<QargoAuthenticationService> logger,
        QargoSettings settings)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _settings = settings;
    }

    public async Task<string> GetAccessTokenAsync(QargoEnvironment environment)
    {
        var cacheKey = $"qargo_token_{environment.BaseUrl}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            _logger.LogDebug("Using cached access token for {Environment}", environment.BaseUrl);
            return cachedToken;
        }

        _logger.LogInformation("Requesting new access token for {Environment}", environment.BaseUrl);
        return await RefreshAccessTokenAsync(environment);
    }

    public async Task<string> RefreshAccessTokenAsync(QargoEnvironment environment)
    {
        try
        {
            var tokenRequest = new AuthTokenRequest
            {
                ClientId = environment.ClientId,
                ClientSecret = environment.ClientSecret
            };

            var json = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var tokenUrl = $"{environment.BaseUrl.TrimEnd('/')}/v1/auth/token";
            
            _logger.LogDebug("Requesting token from {TokenUrl}", tokenUrl);
            
            var response = await _httpClient.PostAsync(tokenUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token request failed with status {StatusCode}: {Error}", 
                    response.StatusCode, error);
                throw new HttpRequestException($"Authentication failed: {response.StatusCode} - {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<AuthTokenResponse>(responseJson);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Invalid token response received");
            }

            // Cache token for 90% of its lifetime to ensure we refresh before expiry
            var cacheExpiry = TimeSpan.FromSeconds(tokenResponse.ExpiresIn * 0.9);
            var cacheKey = $"qargo_token_{environment.BaseUrl}";
            
            _cache.Set(cacheKey, tokenResponse.AccessToken, cacheExpiry);
            
            _logger.LogInformation("Successfully obtained access token for {Environment}, expires in {ExpiresIn}s", 
                environment.BaseUrl, tokenResponse.ExpiresIn);

            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain access token for {Environment}", environment.BaseUrl);
            throw;
        }
    }

    public bool IsTokenValid(QargoEnvironment environment)
    {
        var cacheKey = $"qargo_token_{environment.BaseUrl}";
        return _cache.TryGetValue(cacheKey, out _);
    }
}