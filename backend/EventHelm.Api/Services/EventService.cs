using Dapper;
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

    public async Task<List<Event>> GetAllAsync()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var connection = new NpgsqlConnection(connectionString);

        var events = await connection.QueryAsync<Event>(
            """
            SELECT "Id", "Name", "Status"
            FROM "Events"
            ORDER BY "Name";
            """
        );

        return events.ToList();
    }

    public async Task<Event> CreateAsync(Event eventItem)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var connection = new NpgsqlConnection(connectionString);

        eventItem.Id = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO "Events" ("Id", "Name", "Status")
            VALUES (@Id, @Name, @Status);
            """,
            eventItem
        );

        return eventItem;
    }
}