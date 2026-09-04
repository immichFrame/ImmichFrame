using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ImmichFrame.WebApi.Database;

// Used by `dotnet ef` at design time only
public class SettingsDbContextFactory : IDesignTimeDbContextFactory<SettingsDbContext>
{
    public SettingsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseSqlite("Data Source=design.db")
            .Options;
        return new SettingsDbContext(options);
    }
}
