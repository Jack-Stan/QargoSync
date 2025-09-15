using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QargoSync.App;
using QargoSync.Core;
using QargoSync.Infrastructure;
using QargoSync.Models.Configuration;
using Serilog;

Console.WriteLine("QargoSync - Resource Unavailability Synchronization");
Console.WriteLine("==================================================\n");

// Setup Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/qargosync-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    // Setup configuration with environment variable support
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

    // Setup dependency injection
    var services = new ServiceCollection();
    
    // Add logging
    services.AddLogging(builder => builder.AddSerilog());
    
    // Add HTTP clients
    services.AddHttpClient();
    
    // Add memory cache for authentication service
    services.AddMemoryCache();
    
    // Register configuration
    services.AddSingleton<IConfiguration>(configuration);
    
    // Bind and expand configuration
    var qargoSettings = new QargoSettings();
    configuration.GetSection("QargoSettings").Bind(qargoSettings);
    ExpandEnvironmentVariables(qargoSettings);
    services.AddSingleton(qargoSettings);
    
    var syncSettings = new SynchronizationSettings();
    configuration.GetSection("SynchronizationSettings").Bind(syncSettings);
    services.AddSingleton(syncSettings);
    
    // Register services
    services.AddTransient<IAuthenticationService, QargoAuthenticationService>();
    services.AddTransient<IQargoHttpService, QargoHttpService>();
    services.AddTransient<IResourceRepository, QargoResourceRepository>();
    
    // Register individual environments for QargoSyncRepository
    services.AddTransient<QargoEnvironment>(provider => 
    {
        var settings = provider.GetRequiredService<QargoSettings>();
        return settings.Master; // Default to master, but this will be resolved correctly
    });
    
    // Custom factory for QargoSyncRepository to inject both environments
    services.AddTransient<ISyncRepository>(provider =>
    {
        var httpService = provider.GetRequiredService<IQargoHttpService>();
        var logger = provider.GetRequiredService<ILogger<QargoSyncRepository>>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var settings = provider.GetRequiredService<QargoSettings>();
        
        return new QargoSyncRepository(
            httpService, 
            logger, 
            loggerFactory, 
            settings.Master, 
            settings.Target);
    });
    
    services.AddTransient<ApiExplorer>();
    
    // Build service provider
    var serviceProvider = services.BuildServiceProvider();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Starting QargoSync application");
    
    // Check if we're in API exploration mode or sync mode
    var arguments = Environment.GetCommandLineArgs();
    if (arguments.Contains("--explore") || arguments.Contains("-e"))
    {
        logger.LogInformation("Running in API exploration mode");
        var explorer = serviceProvider.GetRequiredService<ApiExplorer>();
        await explorer.ExploreApiAsync();
    }
    else
    {
        logger.LogInformation("Running synchronization");
        var syncRepository = serviceProvider.GetRequiredService<ISyncRepository>();
        var result = await syncRepository.SynchronizeUnavailabilitiesAsync(syncSettings);
        
        logger.LogInformation("Synchronization completed:");
        logger.LogInformation($"   Resources processed: {result.ResourcesProcessed}");
        logger.LogInformation($"   Created: {result.UnavailabilitiesCreated}");
        logger.LogInformation($"   Updated: {result.UnavailabilitiesUpdated}");
        logger.LogInformation($"   Deleted: {result.UnavailabilitiesDeleted}");
        logger.LogInformation($"   Errors: {result.Errors.Count}");
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Console.WriteLine($"\nError: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    Log.CloseAndFlush();
}

Console.WriteLine("\nQargoSync completed successfully!");
Console.WriteLine("\nUsage:");
Console.WriteLine("  dotnet run                 # Run synchronization");
Console.WriteLine("  dotnet run --explore       # Run API exploration");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

static void ExpandEnvironmentVariables(QargoSettings settings)
{
    // Expand environment variables for Master environment
    if (settings.Master.BaseUrl?.StartsWith("${") == true)
        settings.Master.BaseUrl = GetEnvironmentVariable(settings.Master.BaseUrl);
    if (settings.Master.ClientId?.StartsWith("${") == true)
        settings.Master.ClientId = GetEnvironmentVariable(settings.Master.ClientId);
    if (settings.Master.ClientSecret?.StartsWith("${") == true)
        settings.Master.ClientSecret = GetEnvironmentVariable(settings.Master.ClientSecret);
    
    // Expand environment variables for Target environment
    if (settings.Target.BaseUrl?.StartsWith("${") == true)
        settings.Target.BaseUrl = GetEnvironmentVariable(settings.Target.BaseUrl);
    if (settings.Target.ClientId?.StartsWith("${") == true)
        settings.Target.ClientId = GetEnvironmentVariable(settings.Target.ClientId);
    if (settings.Target.ClientSecret?.StartsWith("${") == true)
        settings.Target.ClientSecret = GetEnvironmentVariable(settings.Target.ClientSecret);
}

static string GetEnvironmentVariable(string variablePlaceholder)
{
    // Extract variable name from ${VARIABLE_NAME} format
    var variableName = variablePlaceholder.Trim('$', '{', '}');
    var value = Environment.GetEnvironmentVariable(variableName);
    
    if (string.IsNullOrEmpty(value))
    {
        throw new InvalidOperationException($"Environment variable '{variableName}' not found. " +
            "Please configure GitHub Secrets or local environment variables.");
    }
    
    return value;
}

Console.WriteLine("\nNext steps:");
Console.WriteLine("1. Check which endpoints returned successful responses");
Console.WriteLine("2. Look for unavailability-related endpoints");
Console.WriteLine("3. Examine the response structure for data mapping");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
