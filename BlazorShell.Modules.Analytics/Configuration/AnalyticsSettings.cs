// BlazorShell.Modules.Analytics/Configuration/AnalyticsSettings.cs
using System.ComponentModel.DataAnnotations;

namespace BlazorShell.Modules.Analytics.Configuration;

public sealed class AnalyticsSettings
{
    public bool UseSeparateDatabase { get; set; } = true;
    public string DatabaseProvider { get; set; } = "SqlServer";
    public bool EnableMigrations { get; set; } = false;
    public bool SeedSampleData { get; set; } = true;

    // Cache
    public bool EnableCaching { get; set; } = true;

    // ConnectionString can be provided via Modules:Analytics:ConnectionString
    // If null, we’ll fall back to DefaultConnection when UseSeparateDatabase=false
    public string? ConnectionString { get; set; }
}
