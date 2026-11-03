using Dapper;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public sealed class EventBoatService
{
    private readonly string _connectionString;

    public EventBoatService(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A DefaultConnection adatbázis-kapcsolat nincs beállítva.");
    }

    public async Task<IReadOnlyList<EventBoat>> GetAllAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                eb."Id",
                eb."EventId",
                eb."BoatId",
                eb."Code",
                eb."DisplayName",
                eb."AreaId",
                eb."TaskId",
                eb."ShiftId",
                eb."RadioChannel",
                eb."TrunkGroup",
                eb."StartAt",
                eb."EndAt",
                eb."Status",
                eb."Notes",
                eb."CreatedAt",
                eb."UpdatedAt",

                b."Name" AS "BoatName",
                b."RegistrationNumber",
                b."CallSign",

                ea."Name" AS "AreaName",
                et."Name" AS "TaskName",
                es."Name" AS "ShiftName"

            FROM "EventBoats" eb

            INNER JOIN "Boats" b
                ON b."Id" = eb."BoatId"

            LEFT JOIN "EventAreas" ea
                ON ea."Id" = eb."AreaId"

            LEFT JOIN "EventTasks" et
                ON et."Id" = eb."TaskId"

            LEFT JOIN "EventShifts" es
                ON es."Id" = eb."ShiftId"

            WHERE eb."EventId" = @EventId

            ORDER BY
                COALESCE(eb."Code", eb."DisplayName", b."Name"),
                eb."CreatedAt";
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

        var eventBoats =
            await connection.QueryAsync<EventBoat>(command);

        return eventBoats.ToList();
    }

    public async Task<EventBoat?> GetByIdAsync(
        Guid eventId,
        Guid eventBoatId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                eb."Id",
                eb."EventId",
                eb."BoatId",
                eb."Code",
                eb."DisplayName",
                eb."AreaId",
                eb."TaskId",
                eb."ShiftId",
                eb."RadioChannel",
                eb."TrunkGroup",
                eb."StartAt",
                eb."EndAt",
                eb."Status",
                eb."Notes",
                eb."CreatedAt",
                eb."UpdatedAt",

                b."Name" AS "BoatName",
                b."RegistrationNumber",
                b."CallSign",

                ea."Name" AS "AreaName",
                et."Name" AS "TaskName",
                es."Name" AS "ShiftName"

            FROM "EventBoats" eb

            INNER JOIN "Boats" b
                ON b."Id" = eb."BoatId"

            LEFT JOIN "EventAreas" ea
                ON ea."Id" = eb."AreaId"

            LEFT JOIN "EventTasks" et
                ON et."Id" = eb."TaskId"

            LEFT JOIN "EventShifts" es
                ON es."Id" = eb."ShiftId"

            WHERE eb."EventId" = @EventId
              AND eb."Id" = @EventBoatId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                EventBoatId = eventBoatId
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<EventBoat>(
            command);
    }

    public async Task<EventBoat> CreateAsync(
        Guid eventId,
        CreateEventBoatRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateEventBoatRequest(
            request.BoatId,
            request.Code,
            request.DisplayName,
            request.StartAt,
            request.EndAt,
            request.Status);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await ValidateReferencesAsync(
            connection,
            eventId,
            request.BoatId,
            request.AreaId,
            request.TaskId,
            request.ShiftId,
            cancellationToken);

        const string sql = """
            INSERT INTO "EventBoats"
            (
                "Id",
                "EventId",
                "BoatId",
                "Code",
                "DisplayName",
                "AreaId",
                "TaskId",
                "ShiftId",
                "RadioChannel",
                "TrunkGroup",
                "StartAt",
                "EndAt",
                "Status",
                "Notes",
                "CreatedAt",
                "UpdatedAt"
            )
            VALUES
            (
                gen_random_uuid(),
                @EventId,
                @BoatId,
                @Code,
                @DisplayName,
                @AreaId,
                @TaskId,
                @ShiftId,
                @RadioChannel,
                @TrunkGroup,
                @StartAt,
                @EndAt,
                @Status,
                @Notes,
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
                request.BoatId,
                Code = NormalizeOptionalText(request.Code),
                DisplayName =
                    NormalizeOptionalText(request.DisplayName),
                request.AreaId,
                request.TaskId,
                request.ShiftId,
                RadioChannel =
                    NormalizeOptionalText(request.RadioChannel),
                TrunkGroup =
                    NormalizeOptionalText(request.TrunkGroup),
                request.StartAt,
                request.EndAt,
                Status = NormalizeRequiredText(
                    request.Status,
                    "Planned"),
                Notes = NormalizeOptionalText(request.Notes)
            },
            cancellationToken: cancellationToken);

        Guid eventBoatId;

        try
        {
            eventBoatId =
                await connection.ExecuteScalarAsync<Guid>(command);
        }
        catch (PostgresException exception)
            when (exception.SqlState == "23505")
        {
            throw new InvalidOperationException(
                "Ezzel a kóddal már létezik hajóbeosztás ezen a rendezvényen.",
                exception);
        }

        return await GetRequiredByIdAsync(
            connection,
            eventId,
            eventBoatId,
            cancellationToken);
    }

    public async Task<EventBoat?> UpdateAsync(
        Guid eventId,
        Guid eventBoatId,
        UpdateEventBoatRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateEventBoatRequest(
            request.BoatId,
            request.Code,
            request.DisplayName,
            request.StartAt,
            request.EndAt,
            request.Status);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await ValidateReferencesAsync(
            connection,
            eventId,
            request.BoatId,
            request.AreaId,
            request.TaskId,
            request.ShiftId,
            cancellationToken);

        const string sql = """
            UPDATE "EventBoats"
            SET
                "BoatId" = @BoatId,
                "Code" = @Code,
                "DisplayName" = @DisplayName,
                "AreaId" = @AreaId,
                "TaskId" = @TaskId,
                "ShiftId" = @ShiftId,
                "RadioChannel" = @RadioChannel,
                "TrunkGroup" = @TrunkGroup,
                "StartAt" = @StartAt,
                "EndAt" = @EndAt,
                "Status" = @Status,
                "Notes" = @Notes,
                "UpdatedAt" = now()

            WHERE "Id" = @EventBoatId
              AND "EventId" = @EventId;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                EventBoatId = eventBoatId,
                EventId = eventId,
                request.BoatId,
                Code = NormalizeOptionalText(request.Code),
                DisplayName =
                    NormalizeOptionalText(request.DisplayName),
                request.AreaId,
                request.TaskId,
                request.ShiftId,
                RadioChannel =
                    NormalizeOptionalText(request.RadioChannel),
                TrunkGroup =
                    NormalizeOptionalText(request.TrunkGroup),
                request.StartAt,
                request.EndAt,
                Status = NormalizeRequiredText(
                    request.Status,
                    "Planned"),
                Notes = NormalizeOptionalText(request.Notes)
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
                "Ezzel a kóddal már létezik hajóbeosztás ezen a rendezvényen.",
                exception);
        }

        if (affectedRows == 0)
        {
            return null;
        }

        return await GetRequiredByIdAsync(
            connection,
            eventId,
            eventBoatId,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid eventId,
        Guid eventBoatId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM "EventBoats"
            WHERE "Id" = @EventBoatId
              AND "EventId" = @EventId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        var command = new CommandDefinition(
            sql,
            new
            {
                EventBoatId = eventBoatId,
                EventId = eventId
            },
            cancellationToken: cancellationToken);

        var affectedRows =
            await connection.ExecuteAsync(command);

        return affectedRows > 0;
    }

    private static async Task ValidateReferencesAsync(
        NpgsqlConnection connection,
        Guid eventId,
        Guid boatId,
        Guid? areaId,
        Guid? taskId,
        Guid? shiftId,
        CancellationToken cancellationToken)
    {
        const string eventExistsSql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM "Events"
                WHERE "Id" = @EventId
            );
            """;

        var eventExists =
            await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    eventExistsSql,
                    new
                    {
                        EventId = eventId
                    },
                    cancellationToken: cancellationToken));

        if (!eventExists)
        {
            throw new KeyNotFoundException(
                "A megadott rendezvény nem található.");
        }

        const string boatExistsSql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM "Boats"
                WHERE "Id" = @BoatId
            );
            """;

        var boatExists =
            await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    boatExistsSql,
                    new
                    {
                        BoatId = boatId
                    },
                    cancellationToken: cancellationToken));

        if (!boatExists)
        {
            throw new KeyNotFoundException(
                "A megadott motorcsónak nem található.");
        }

        if (areaId.HasValue)
        {
            const string areaExistsSql = """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM "EventAreas"
                    WHERE "Id" = @AreaId
                      AND "EventId" = @EventId
                );
                """;

            var areaExists =
                await connection.ExecuteScalarAsync<bool>(
                    new CommandDefinition(
                        areaExistsSql,
                        new
                        {
                            AreaId = areaId.Value,
                            EventId = eventId
                        },
                        cancellationToken:
                            cancellationToken));

            if (!areaExists)
            {
                throw new InvalidOperationException(
                    "A kiválasztott terület nem ehhez a rendezvényhez tartozik.");
            }
        }

        if (taskId.HasValue)
        {
            const string taskExistsSql = """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM "EventTasks"
                    WHERE "Id" = @TaskId
                      AND "EventId" = @EventId
                );
                """;

            var taskExists =
                await connection.ExecuteScalarAsync<bool>(
                    new CommandDefinition(
                        taskExistsSql,
                        new
                        {
                            TaskId = taskId.Value,
                            EventId = eventId
                        },
                        cancellationToken:
                            cancellationToken));

            if (!taskExists)
            {
                throw new InvalidOperationException(
                    "A kiválasztott feladat nem ehhez a rendezvényhez tartozik.");
            }
        }

        if (shiftId.HasValue)
        {
            const string shiftExistsSql = """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM "EventShifts"
                    WHERE "Id" = @ShiftId
                      AND "EventId" = @EventId
                );
                """;

            var shiftExists =
                await connection.ExecuteScalarAsync<bool>(
                    new CommandDefinition(
                        shiftExistsSql,
                        new
                        {
                            ShiftId = shiftId.Value,
                            EventId = eventId
                        },
                        cancellationToken:
                            cancellationToken));

            if (!shiftExists)
            {
                throw new InvalidOperationException(
                    "A kiválasztott műszak nem ehhez a rendezvényhez tartozik.");
            }
        }
    }

    private static async Task<EventBoat> GetRequiredByIdAsync(
        NpgsqlConnection connection,
        Guid eventId,
        Guid eventBoatId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                eb."Id",
                eb."EventId",
                eb."BoatId",
                eb."Code",
                eb."DisplayName",
                eb."AreaId",
                eb."TaskId",
                eb."ShiftId",
                eb."RadioChannel",
                eb."TrunkGroup",
                eb."StartAt",
                eb."EndAt",
                eb."Status",
                eb."Notes",
                eb."CreatedAt",
                eb."UpdatedAt",

                b."Name" AS "BoatName",
                b."RegistrationNumber",
                b."CallSign",

                ea."Name" AS "AreaName",
                et."Name" AS "TaskName",
                es."Name" AS "ShiftName"

            FROM "EventBoats" eb

            INNER JOIN "Boats" b
                ON b."Id" = eb."BoatId"

            LEFT JOIN "EventAreas" ea
                ON ea."Id" = eb."AreaId"

            LEFT JOIN "EventTasks" et
                ON et."Id" = eb."TaskId"

            LEFT JOIN "EventShifts" es
                ON es."Id" = eb."ShiftId"

            WHERE eb."EventId" = @EventId
              AND eb."Id" = @EventBoatId;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                EventBoatId = eventBoatId
            },
            cancellationToken: cancellationToken);

        var eventBoat =
            await connection.QuerySingleOrDefaultAsync<EventBoat>(
                command);

        return eventBoat
            ?? throw new InvalidOperationException(
                "A hajóbeosztás létrejött, de nem sikerült visszaolvasni.");
    }

    private static void ValidateEventBoatRequest(
        Guid boatId,
        string? code,
        string? displayName,
        DateTimeOffset? startAt,
        DateTimeOffset? endAt,
        string? status)
    {
        if (boatId == Guid.Empty)
        {
            throw new ArgumentException(
                "A motorcsónak kiválasztása kötelező.");
        }

        if (code?.Trim().Length > 100)
        {
            throw new ArgumentException(
                "A hajóbeosztás kódja legfeljebb 100 karakter lehet.");
        }

        if (displayName?.Trim().Length > 200)
        {
            throw new ArgumentException(
                "A megjelenítési név legfeljebb 200 karakter lehet.");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException(
                "A hajóbeosztás állapota kötelező.");
        }

        if (
            startAt.HasValue &&
            endAt.HasValue &&
            endAt.Value <= startAt.Value)
        {
            throw new ArgumentException(
                "A befejezési időpontnak későbbinek kell lennie a kezdési időpontnál.");
        }
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string NormalizeRequiredText(
        string? value,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }
}