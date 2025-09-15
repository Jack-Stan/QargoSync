using Microsoft.Extensions.Configuration;
using QargoSync.Models.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QargoSync.App;

public class ApiExplorer
{
    private readonly QargoSettings _settings;

    public ApiExplorer(IConfiguration configuration)
    {
        _settings = configuration.GetSection("QargoSettings").Get<QargoSettings>() ?? new QargoSettings();
    }

    public async Task ExploreApiAsync()
    {
        Console.WriteLine("Exploring Qargo API Environments...");
        Console.WriteLine("====================================");

        // Test Master Environment (source data)
        Console.WriteLine("\nTesting MASTER Environment (Source):");
        await TestEnvironmentAsync(_settings.Master, "Master");

        Console.WriteLine("\n" + new string('=', 50));

        // Test Target Environment (your environment)
        Console.WriteLine("\nTesting TARGET Environment (Your Environment):");
        await TestEnvironmentAsync(_settings.Target, "Target");
    }

    private async Task TestEnvironmentAsync(QargoEnvironment environment, string environmentName)
    {
        if (string.IsNullOrEmpty(environment.ClientId) || string.IsNullOrEmpty(environment.ClientSecret))
        {
            Console.WriteLine($"ERROR: {environmentName} environment credentials not configured!");
            return;
        }

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(environment.BaseUrl);

        Console.WriteLine($"Environment: {environmentName}");
        Console.WriteLine($"Base URL: {environment.BaseUrl}");
        Console.WriteLine($"Client ID: {environment.ClientId}");
        Console.WriteLine();

        // First try OAuth2 token endpoint
        var tokenResult = await GetOAuth2TokenAsync(httpClient, environment);
        if (tokenResult != null)
        {
            Console.WriteLine("OAuth2 Authentication successful!");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult);
            
            // Now test API endpoints with proper authentication
            await TestApiEndpointsAsync(httpClient);
        }
        else
        {
            Console.WriteLine("OAuth2 Authentication failed - trying basic endpoints");
            // Test basic endpoints without authentication
            await TestBasicEndpointsAsync(httpClient);
        }
    }

    private async Task<string?> GetOAuth2TokenAsync(HttpClient httpClient, QargoEnvironment environment)
    {
        try
        {
            // According to Qargo API docs, authentication should be done against the main API endpoint
            // using HTTP Basic Auth with client_id:client_secret
            var tokenUrl = "https://api.qargo.com/v1/auth/token";
            
            Console.WriteLine($"Trying token endpoint: {tokenUrl}");
            
            // Create HTTP request with Basic Auth
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            
            // Set up Basic Authentication header
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{environment.ClientId}:{environment.ClientSecret}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            
            // Set content type (empty body for this request)
            request.Content = new StringContent("", Encoding.UTF8, "application/json");
            
            var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (tokenResponse.TryGetProperty("access_token", out var accessToken))
                {
                    Console.WriteLine($"Token obtained from {tokenUrl}");
                    return accessToken.GetString();
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token request failed: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OAuth2 token request failed: {ex.Message}");
        }
        
        return null;
    }

    private async Task TestApiEndpointsAsync(HttpClient httpClient)
    {
        var endpointsToTest = new[]
        {
            // Core resource endpoints (our main use case)
            "/v1/resources/resource",
            "/v1/resources/resource?limit=10",
            
            // Authentication (should work)
            "/v1/auth/token",
            
            // Orders endpoints
            "/v1/orders/order/upload",
            
            // Accounting endpoints  
            "/v1/accounting/sync-tasks",
            "/v1/accounting/company",
            
            // Task endpoints
            "/v1/tasks/available-tasks",
            
            // Document endpoints (test with example UUID)
            "/v1/documents/document/00000000-0000-0000-0000-000000000001/download",
            
            // Webhook endpoints (will likely be POST only, expect 405 Method Not Allowed)
            "/v1/webhook",
            "/v1/webhook/fleet-status-update",
            "/v1/webhook/subco-status-update",
            
            // API exploration - these might not exist but worth testing
            "/docs",
            "/openapi.json",
            "/v1/health", 
            "/v1/status",
            "/v1/info",
            "/v1/version"
        };

        Console.WriteLine("Testing authenticated API endpoints:");
        Console.WriteLine("OK = Working endpoint | NOT FOUND = Not found | METHOD NOT ALLOWED = Method not allowed");
        Console.WriteLine();
        foreach (var endpoint in endpointsToTest)
        {
            await TestEndpointAsync(httpClient, endpoint);
            await Task.Delay(200);
        }
    }

    private async Task TestBasicEndpointsAsync(HttpClient httpClient)
    {
        var endpointsToTest = new[]
        {
            "/",
            "/docs",
            "/openapi.json",
            "/api",
            "/health"
        };

        Console.WriteLine("Testing basic endpoints:");
        foreach (var endpoint in endpointsToTest)
        {
            await TestEndpointAsync(httpClient, endpoint);
            await Task.Delay(200);
        }
    }

    private async Task TestEndpointAsync(HttpClient httpClient, string endpoint)
    {
        try
        {
            Console.Write($"Testing: {endpoint} ");
            
            var response = await httpClient.GetAsync(endpoint);
            
            // Add visual indicator for status
            string statusIndicator = response.StatusCode switch
            {
                System.Net.HttpStatusCode.OK => "OK",
                System.Net.HttpStatusCode.NotFound => "NOT FOUND",
                System.Net.HttpStatusCode.MethodNotAllowed => "METHOD NOT ALLOWED",
                System.Net.HttpStatusCode.Unauthorized => "UNAUTHORIZED",
                System.Net.HttpStatusCode.Forbidden => "FORBIDDEN",
                _ => "OTHER"
            };
            
            Console.WriteLine($"{statusIndicator} {(int)response.StatusCode} {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.Content.Headers.ContentType?.MediaType == "application/json" && !string.IsNullOrEmpty(content))
                {
                    try
                    {
                        var jsonDocument = JsonDocument.Parse(content);
                        var prettyJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = true });
                        
                        // Show first 200 characters of JSON response
                        var preview = prettyJson.Length > 200 ? prettyJson.Substring(0, 200) + "..." : prettyJson;
                        Console.WriteLine($"  JSON Response preview:\n{preview}");
                    }
                    catch
                    {
                        var preview = content.Length > 100 ? content.Substring(0, 100) + "..." : content;
                        Console.WriteLine($"  Response preview: {preview}");
                    }
                }
                else if (!string.IsNullOrEmpty(content))
                {
                    var preview = content.Length > 100 ? content.Substring(0, 100) + "..." : content;
                    Console.WriteLine($"  Response preview: {preview}");
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                Console.WriteLine($"  Note: Endpoint exists but GET method not allowed (probably POST/PUT only)");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // No additional output for 404s - they're expected during discovery
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(errorContent) && errorContent.Length < 200)
                {
                    Console.WriteLine($"  Error: {errorContent}");
                }
            }
            
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Exception: {ex.Message}");
            Console.WriteLine();
        }
    }
}