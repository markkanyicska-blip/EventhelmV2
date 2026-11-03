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

    private const string SelectColumns =
        """
        "Id",
        "Name",
        "RegistrationNumber",
        "CallSign",
        "Type",
        "Capacity",
        "Manufacturer",
        "Engine",
        "EnginePower",
        "FuelType",
        "Status",
        "Notes",
        "CreatedAt"
        """;

    public async Task<IReadOnlyList<Boat>> GetAllAsync()
    {
        await using var connection = CreateConnection();

        var boats = await connection.QueryAsync<Boat>(
            $"""
            SELECT
                {SelectColumns}
            FROM "Boats"
            ORDER BY "CallSign", "Name";
            """);

        return boats.ToList();
    }

    public async Task<Boat?> GetByIdAsync(Guid id)
    {
        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<Boat>(
            $"""
            SELECT
                {SelectColumns}
            FROM "Boats"
            WHERE "Id" = @Id;
            """,
            new { Id = id });
    }

    public async Task<Boat> CreateAsync(Boat boat)
    {
        ApplyDefaults(boat);

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
                "Type",
                "Capacity",
                "Manufacturer",
                "Engine",
                "EnginePower",
                "FuelType",
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
                @Type,
                @Capacity,
                @Manufacturer,
                @Engine,
                @EnginePower,
                @FuelType,
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
        ApplyDefaults(boat);

        await using var connection = CreateConnection();

        var rowsAffected = await connection.ExecuteAsync(
            """
            UPDATE "Boats"
            SET
                "Name" = @Name,
                "RegistrationNumber" = @RegistrationNumber,
                "CallSign" = @CallSign,
                "Type" = @Type,
                "Capacity" = @Capacity,
                "Manufacturer" = @Manufacturer,
                "Engine" = @Engine,
                "EnginePower" = @EnginePower,
                "FuelType" = @FuelType,
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
                boat.Type,
                boat.Capacity,
                boat.Manufacturer,
                boat.Engine,
                boat.EnginePower,
                boat.FuelType,
                boat.Status,
                boat.Notes
            });

        if (rowsAffected == 0)
            return null;

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

    private static void ApplyDefaults(Boat boat)
    {
        boat.Name = boat.Name.Trim();
        boat.RegistrationNumber = boat.RegistrationNumber.Trim();
        boat.CallSign = boat.CallSign.Trim();

        boat.Type = Normalize(boat.Type);
        boat.Manufacturer = Normalize(boat.Manufacturer);
        boat.Engine = Normalize(boat.Engine);
        boat.FuelType = Normalize(boat.FuelType);

        boat.Status =
            string.IsNullOrWhiteSpace(boat.Status)
                ? "Active"
                : boat.Status.Trim();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}