using Dapper;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public class BoatService : IBoatService
{
    private readonly IConfiguration _configuration;

    public BoatService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A DefaultConnection nincs beállítva az appsettings.json fájlban.");

        return new NpgsqlConnection(connectionString);
    }

    public async Task<IReadOnlyList<Boat>> GetAllAsync()
    {
        await using var connection = CreateConnection();

        var boats = await connection.QueryAsync<Boat>(
            """
            SELECT
                "Id",
                "Name",
                "RegistrationNumber",
                "CallSign",
                "Status",
                "Notes",
                "CreatedAt"
            FROM "Boats"
            ORDER BY "CallSign", "Name";
            """);

        return boats.ToList();
    }

    public async Task<Boat?> GetByIdAsync(Guid id)
    {
        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<Boat>(
            """
            SELECT
                "Id",
                "Name",
                "RegistrationNumber",
                "CallSign",
                "Status",
                "Notes",
                "CreatedAt"
            FROM "Boats"
            WHERE "Id" = @Id;
            """,
            new { Id = id });
    }

    public async Task<Boat> CreateAsync(Boat boat)
    {
        await using var connection = CreateConnection();

        boat.Id = Guid.NewGuid();
        boat.CreatedAt = DateTime.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO "Boats"
            (
                "Id",
                "Name",
                "RegistrationNumber",
                "CallSign",
                "Status",
                "Notes",
                "CreatedAt"
            )
            VALUES
            (
                @Id,
                @Name,
                @RegistrationNumber,
                @CallSign,
                @Status,
                @Notes,
                @CreatedAt
            );
            """,
            boat);

        return boat;
    }

    public async Task<Boat?> UpdateAsync(Guid id, Boat boat)
    {
        await using var connection = CreateConnection();

        var rowsAffected = await connection.ExecuteAsync(
            """
            UPDATE "Boats"
            SET
                "Name" = @Name,
                "RegistrationNumber" = @RegistrationNumber,
                "CallSign" = @CallSign,
                "Status" = @Status,
                "Notes" = @Notes
            WHERE "Id" = @Id;
            """,
            new
            {
                Id = id,
                boat.Name,
                boat.RegistrationNumber,
                boat.CallSign,
                boat.Status,
                boat.Notes
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
            DELETE FROM "Boats"
            WHERE "Id" = @Id;
            """,
            new { Id = id });

        return rowsAffected > 0;
    }
}