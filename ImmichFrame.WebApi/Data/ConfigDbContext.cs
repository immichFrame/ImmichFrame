using ImmichFrame.WebApi.Data.Converters;
using ImmichFrame.WebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmichFrame.WebApi.Data;

public class ConfigDbContext(DbContextOptions<ConfigDbContext> options) : DbContext(options)
{
    public DbSet<AccountOverrideEntity> AccountOverrides => Set<AccountOverrideEntity>();

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
    }
}


