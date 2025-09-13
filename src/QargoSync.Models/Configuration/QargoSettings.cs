namespace QargoSync.Models.Configuration;

public class QargoSettings
{
    public QargoEnvironment MasterEnvironment { get; set; } = new();
    public QargoEnvironment TargetEnvironment { get; set; } = new();
}

public class QargoEnvironment
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class SynchronizationSettings
{
    public int Year { get; set; } = 2025;
    public DateTime StartDate { get; set; } = new DateTime(2025, 1, 1);
    public DateTime EndDate { get; set; } = new DateTime(2025, 12, 31, 23, 59, 59);
    public int IntervalMinutes { get; set; } = 30;
    public int BatchSize { get; set; } = 100;
    public bool DryRun { get; set; } = false;
}