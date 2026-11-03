using Dapper;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public sealed class EventAreaService
{
    private readonly string _connectionString;

    public EventAreaService(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A DefaultConnection adatbázis-kapcsolat nincs beállítva.");
    }

    public async Task<IReadOnlyList<EventArea>> GetAllAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                "Id",
                "EventId",
                "Name",
                "AreaType",
                "Latitude",
                "Longitude",
                "RadiusMeters",
                "Description",
                "SortOrder",
                "IsActive",
                "CreatedAt",
                "UpdatedAt"
            FROM "EventAreas"
            WHERE "EventId" = @EventId
            ORDER BY
                "SortOrder",
                "Name";
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId
            },
            cancellationToken: cancellationToken);

        var areas =
            await connection.QueryAsync<EventArea>(command);

        return areas.ToList();
    }

    public async Task<EventArea?> GetByIdAsync(
        Guid eventId,
        Guid areaId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                "Id",
                "EventId",
                "Name",
                "AreaType",
                "Latitude",
                "Longitude",
                "RadiusMeters",
                "Description",
                "SortOrder",
                "IsActive",
                "CreatedAt",
                "UpdatedAt"
            FROM "EventAreas"
            WHERE "EventId" = @EventId
              AND "Id" = @AreaId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                AreaId = areaId
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<EventArea>(
            command);
    }

    public async Task<EventArea> CreateAsync(
        Guid eventId,
        CreateEventAreaRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(
            request.Name,
            request.Latitude,
            request.Longitude,
            request.RadiusMeters);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await EnsureEventExistsAsync(
            connection,
            eventId,
            cancellationToken);

        const string sql = """
            INSERT INTO "EventAreas"
            (
                "Id",
                "EventId",
                "Name",
                "AreaType",
                "Latitude",
                "Longitude",
                "RadiusMeters",
                "Description",
                "SortOrder",
                "IsActive",
                "CreatedAt",
                "UpdatedAt"
            )
            VALUES
            (
                gen_random_uuid(),
                @EventId,
                @Name,
                @AreaType,
                @Latitude,
                @Longitude,
                @RadiusMeters,
                @Description,
                @SortOrder,
                @IsActive,
                now(),
                now()
            )
            RETURNING "Id";
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                Name = request.Name.Trim(),
                AreaType = NormalizeOptionalText(
                    request.AreaType),
                request.Latitude,
                request.Longitude,
                request.RadiusMeters,
                Description = NormalizeOptionalText(
                    request.Description),
                request.SortOrder,
                request.IsActive
            },
            cancellationToken: cancellationToken);

        Guid areaId;

        try
        {
            areaId =
                await connection.ExecuteScalarAsync<Guid>(
                    command);
        }
        catch (PostgresException exception)
            when (exception.SqlState == "23505")
        {
            throw new InvalidOperationException(
                "Ezzel a névvel már létezik terület ezen a rendezvényen.",
                exception);
        }

        return await GetRequiredByIdAsync(
            connection,
            eventId,
            areaId,
            cancellationToken);
    }

    public async Task<EventArea?> UpdateAsync(
        Guid eventId,
        Guid areaId,
        UpdateEventAreaRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(
            request.Name,
            request.Latitude,
            request.Longitude,
            request.RadiusMeters);

        const string sql = """
            UPDATE "EventAreas"
            SET
                "Name" = @Name,
                "AreaType" = @AreaType,
                "Latitude" = @Latitude,
                "Longitude" = @Longitude,
                "RadiusMeters" = @RadiusMeters,
                "Description" = @Description,
                "SortOrder" = @SortOrder,
                "IsActive" = @IsActive,
                "UpdatedAt" = now()
            WHERE "EventId" = @EventId
              AND "Id" = @AreaId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                AreaId = areaId,
                Name = request.Name.Trim(),
                AreaType = NormalizeOptionalText(
                    request.AreaType),
                request.Latitude,
                request.Longitude,
                request.RadiusMeters,
                Description = NormalizeOptionalText(
                    request.Description),
                request.SortOrder,
                request.IsActive
            },
            cancellationToken: cancellationToken);

        int affectedRows;

        try
        {
            affectedRows =
                await connection.ExecuteAsync(command);
        }
        catch (PostgresException exception)
            when (exception.SqlState == "23505")
        {
            throw new InvalidOperationException(
                "Ezzel a névvel már létezik terület ezen a rendezvényen.",
                exception);
        }

        if (affectedRows == 0)
        {
            return null;
        }

        return await GetRequiredByIdAsync(
            connection,
            eventId,
            areaId,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid eventId,
        Guid areaId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM "EventAreas"
            WHERE "EventId" = @EventId
              AND "Id" = @AreaId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                AreaId = areaId
            },
            cancellationToken: cancellationToken);

        try
        {
            var affectedRows =
                await connection.ExecuteAsync(command);

            return affectedRows > 0;
        }
        catch (PostgresException exception)
            when (exception.SqlState == "23503")
        {
            throw new InvalidOperationException(
                "A terület nem törölhető, mert hajóbeosztás használja.",
                exception);
        }
    }

    private static async Task EnsureEventExistsAsync(
        NpgsqlConnection connection,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM "Events"
                WHERE "Id" = @EventId
            );
            """;

        var exists =
            await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        EventId = eventId
                    },
                    cancellationToken: cancellationToken));

        if (!exists)
        {
            throw new KeyNotFoundException(
                "A megadott rendezvény nem található.");
        }
    }

    private static async Task<EventArea> GetRequiredByIdAsync(
        NpgsqlConnection connection,
        Guid eventId,
        Guid areaId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                "Id",
                "EventId",
                "Name",
                "AreaType",
                "Latitude",
                "Longitude",
                "RadiusMeters",
                "Description",
                "SortOrder",
                "IsActive",
                "CreatedAt",
                "UpdatedAt"
            FROM "EventAreas"
            WHERE "EventId" = @EventId
              AND "Id" = @AreaId;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                AreaId = areaId
            },
            cancellationToken: cancellationToken);

        var area =
            await connection.QuerySingleOrDefaultAsync<EventArea>(
                command);

        return area
            ?? throw new InvalidOperationException(
                "A terület létrejött, de nem sikerült visszaolvasni.");
    }

    private static void ValidateRequest(
        string? name,
        decimal? latitude,
        decimal? longitude,
        int? radiusMeters)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A terület neve kötelező.");
        }

        if (name.Trim().Length > 200)
        {
            throw new ArgumentException(
                "A terület neve legfeljebb 200 karakter lehet.");
        }

        if (
            latitude.HasValue &&
            (latitude.Value < -90 ||
             latitude.Value > 90))
        {
            throw new ArgumentException(
                "A szélességi koordinátának -90 és 90 között kell lennie.");
        }

        if (
            longitude.HasValue &&
            (longitude.Value < -180 ||
             longitude.Value > 180))
        {
            throw new ArgumentException(
                "A hosszúsági koordinátának -180 és 180 között kell lennie.");
        }

        if (
            radiusMeters.HasValue &&
            radiusMeters.Value < 0)
        {
            throw new ArgumentException(
                "A sugár nem lehet negatív.");
        }
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}