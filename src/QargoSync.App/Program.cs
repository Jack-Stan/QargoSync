using Microsoft.Extensions.Configuration;
using QargoSync.App;

Console.WriteLine("QargoSync - API Explorer");
Console.WriteLine("========================\n");

// Setup configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Create and run API explorer
var explorer = new ApiExplorer(configuration);
await explorer.ExploreApiAsync();

Console.WriteLine("\nNext steps:");
Console.WriteLine("1. Check which endpoints returned successful responses");
Console.WriteLine("2. Look for unavailability-related endpoints");
Console.WriteLine("3. Examine the response structure for data mapping");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
