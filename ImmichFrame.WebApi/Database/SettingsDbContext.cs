using Microsoft.EntityFrameworkCore;

namespace ImmichFrame.WebApi.Database;

public class SettingsDbContext : DbContext
{
    public SettingsDbContext(DbContextOptions<SettingsDbContext> options) : base(options) { }

    public DbSet<SettingsDocument> SettingsDocuments => Set<SettingsDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SettingsDocument>()
            .Property(d => d.Version)
            .IsConcurrencyToken();
    }
}

public class SettingsDocument
{
    public int Id { get; set; }

    // Raw ServerSettings V2 JSON, pre-Validate (ApiKeyFile stays unresolved)
    public string Json { get; set; } = "{}";

    public int SchemaVersion { get; set; } = 2;

    public DateTime UpdatedAtUtc { get; set; }

    // "Settings.json" | "Settings.yml" | "env" | null when created via admin UI
    public string? ImportedFrom { get; set; }

    public long Version { get; set; }
}
