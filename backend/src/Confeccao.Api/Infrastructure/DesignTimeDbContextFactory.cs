using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Confeccao.Api.Infrastructure;

/// <summary>
/// Used by `dotnet ef migrations add` at design time. Production runtime uses the
/// connection string from configuration via <see cref="Program"/>.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ConfeccaoDbContext>
{
    public ConfeccaoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ConfeccaoDbContext>();
        optionsBuilder
            .UseNpgsql("Host=localhost;Port=5433;Database=confeccao;Username=confeccao;Password=confeccao")
            .UseSnakeCaseNamingConvention();
        return new ConfeccaoDbContext(optionsBuilder.Options);
    }
}
