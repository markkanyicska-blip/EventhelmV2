using Dapper;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public class EventService : IEventService
{
    private readonly IConfiguration _configuration;

    public EventService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A DefaultConnection nincs beállítva.");

        return new NpgsqlConnection(connectionString);
    }

    public async Task<IReadOnlyList<Event>> GetAllAsync()
    {
        await using var connection = CreateConnection();

        var events = await connection.QueryAsync<Event>(
            """
            SELECT
                "Id",
                "Name",
                "Status"
            FROM "Events"
            ORDER BY "Name";
            """);

        return events.ToList();
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<Event>(
            """
            SELECT
                "Id",
                "Name",
                "Status"
            FROM "Events"
            WHERE "Id" = @Id;
            """,
            new { Id = id });
    }

    public async Task<Event> CreateAsync(Event eventItem)
    {
        await using var connection = CreateConnection();

        eventItem.Id = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO "Events"
            (
                "Id",
                "Name",
                "Status"
            )
            VALUES
            (
                @Id,
                @Name,
                @Status
            );
            """,
            eventItem);

        return eventItem;
    }

    public async Task<Event?> UpdateAsync(
        Guid id,
        Event eventItem)
    {
        await using var connection = CreateConnection();

        var rowsAffected = await connection.ExecuteAsync(
            """
            UPDATE "Events"
            SET
                "Name" = @Name,
                "Status" = @Status
            WHERE "Id" = @Id;
            """,
            new
            {
                Id = id,
                eventItem.Name,
                eventItem.Status
            });

        if (rowsAffected == 0)
        {
            return null;
        }

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = CreateConnection();

        var rowsAffected = await connection.ExecuteAsync(
            """
            DELETE FROM "Events"
            WHERE "Id" = @Id;
            """,
            new { Id = id });

        return rowsAffected > 0;
    }
}