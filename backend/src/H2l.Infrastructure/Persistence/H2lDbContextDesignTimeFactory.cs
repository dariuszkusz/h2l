using H2l.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace H2l.Infrastructure.Persistence;

public sealed class H2lDbContextDesignTimeFactory : IDesignTimeDbContextFactory<H2lDbContext>
{
    public H2lDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("H2L_DB_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("Database__ConnectionString")
            ?? "Host=localhost;Port=5432;Database=h2l;Username=h2l;Password=h2l";

        var optionsBuilder = new DbContextOptionsBuilder<H2lDbContext>();
        optionsBuilder
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "app"))
            .UseSnakeCaseNamingConvention();

        return new H2lDbContext(optionsBuilder.Options, new DefaultTenantContext());
    }
}
