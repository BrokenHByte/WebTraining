using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IntegrationTests;

public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; private set; } = null!;
    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync()
    {
        Container = new PostgreSqlBuilder("postgres:16-alpine").Build();

        await Container.StartAsync();
        var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.StopAsync();
        await Container.DisposeAsync();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString).UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        var context = CreateContext();
        // Удаление через EnsureDeleted и применение миграции заново не работает. Ошибка открытого соединения, проще TRUNCATE
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE bookings, events, users RESTART IDENTITY CASCADE");
    }
}

[CollectionDefinition("Postgres Collection")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}