using ImmichFrame.WebApi.Data.Converters;
using ImmichFrame.WebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmichFrame.WebApi.Data;

public class ConfigDbContext(DbContextOptions<ConfigDbContext> options) : DbContext(options)
{
    public DbSet<AccountOverrideEntity> AccountOverrides => Set<AccountOverrideEntity>();
    public DbSet<GeneralSettingsEntity> GeneralSettings => Set<GeneralSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<AccountOverrideEntity>();
        entity.ToTable("AccountOverrides");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.UpdatedAtUtc).IsRequired();

        // Lists stored as JSON text
        entity.Property(x => x.Albums).HasConversion(new JsonValueConverter<List<Guid>>());
        entity.Property(x => x.ExcludedAlbums).HasConversion(new JsonValueConverter<List<Guid>>());
        entity.Property(x => x.People).HasConversion(new JsonValueConverter<List<Guid>>());

        var general = modelBuilder.Entity<GeneralSettingsEntity>();
        general.ToTable("GeneralSettings");
        general.HasKey(x => x.Id);

        general.Property(x => x.Language).IsRequired();
        general.Property(x => x.Style).IsRequired();
        general.Property(x => x.Layout).IsRequired();
        general.Property(x => x.UpdatedAtUtc).IsRequired();

        // List stored as JSON text
        general.Property(x => x.Webcalendars).HasConversion(new JsonValueConverter<List<string>>());
    }
}


