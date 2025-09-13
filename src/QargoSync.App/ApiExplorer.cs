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
        _settings = configuration.GetSection("Qargo").Get<QargoSettings>() ?? new QargoSettings();
    }

    public async Task ExploreApiAsync()
    {
        Console.WriteLine("Exploring Qargo API Environments...");
        Console.WriteLine("====================================");

        // Test Master Environment (source data)
        Console.WriteLine("\nTesting MASTER Environment (Source):");
        await TestEnvironmentAsync(_settings.MasterEnvironment, "Master");

        Console.WriteLine("\n" + new string('=', 50));

        // Test Target Environment (your environment)
        Console.WriteLine("\nTesting TARGET Environment (Your Environment):");
        await TestEnvironmentAsync(_settings.TargetEnvironment, "Target");
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

        // Setup authentication
        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{environment.ClientId}:{environment.ClientSecret}")
        );
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        Console.WriteLine($"Environment: {environmentName}");
        Console.WriteLine($"Base URL: {environment.BaseUrl}");
        Console.WriteLine($"Client ID: {environment.ClientId}");
        Console.WriteLine();

        var endpointsToTest = new[]
        {
            "/",
            "/docs",
            "/openapi.json",
            "/api",
            "/api/v1",
            "/health",
            "/api/health",
            "/api/master-data/drivers",
            "/api/master-data/assets", 
            "/api/master-data/unavailabilities",
            "/api/drivers",
            "/api/assets",
            "/api/unavailabilities",
            "/api/resources",
            "/api/fleet",
            "/api/vehicles"
        };

        foreach (var endpoint in endpointsToTest)
        {
            await TestEndpointAsync(httpClient, endpoint);
            await Task.Delay(300); // Be nice to the API
        }
    }

    private async Task TestEndpointAsync(HttpClient httpClient, string endpoint)
    {
        try
        {
            Console.WriteLine($"Testing: {endpoint}");
            
            var response = await httpClient.GetAsync(endpoint);
            
            Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.Content.Headers.ContentType?.MediaType == "application/json" && !string.IsNullOrEmpty(content))
                {
                    try
                    {
                        var jsonDocument = JsonDocument.Parse(content);
                        var prettyJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = true });
                        
                        // Show first 300 characters
                        var preview = prettyJson.Length > 300 ? prettyJson.Substring(0, 300) + "..." : prettyJson;
                        Console.WriteLine($"  Response preview:\n{preview}");
                    }
                    catch
                    {
                        var preview = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
                        Console.WriteLine($"  Response: {preview}");
                    }
                }
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"  Error: {content}");
            }
            
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Exception: {ex.Message}\n");
        }
    }
}