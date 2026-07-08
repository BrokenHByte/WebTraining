using Microsoft.EntityFrameworkCore;

namespace IntegrationTests;

[Collection("Postgres Collection")]
public class TestMigration(PostgresFixture fixture)
{
    [Fact]
    public async Task MigrationTest()
    {
        await fixture.ResetDatabaseAsync();
        await using var context = fixture.CreateContext();

        var results = await context.Database
            .SqlQuery<string>(
                $"SELECT table_name FROM information_schema.tables WHERE table_name IN ('events', 'bookings') AND table_schema = 'public'")
            .ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.Contains("events", results);
        Assert.Contains("bookings", results);

        var fkExists = await context.Database
            .SqlQuery<string>(
                $"SELECT tc.constraint_name FROM information_schema.table_constraints tc WHERE table_name = 'bookings' AND constraint_type = 'FOREIGN KEY'")
            .ToListAsync();

        Assert.Contains("fk_bookings_events_event_id", fkExists);
    }
}