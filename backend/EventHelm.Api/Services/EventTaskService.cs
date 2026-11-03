using Dapper;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public sealed class EventTaskService
{
    private readonly string _connectionString;

    public EventTaskService(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A DefaultConnection adatbázis-kapcsolat nincs beállítva.");
    }

    public async Task<IReadOnlyList<EventTask>> GetAllAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                "Id",
                "EventId",
                "Name",
                "Code",
                "Description",
                "RequiredOfficials",
                "RequiredBoats",
                "RequiredQualification",
                "Priority",
                "Notes",
                "CreatedAt",
                "UpdatedAt"
            FROM "EventTasks"
            WHERE "EventId" = @EventId
            ORDER BY
                "Priority",
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

        var tasks =
            await connection.QueryAsync<EventTask>(command);

        return tasks.ToList();
    }

    public async Task<EventTask?> GetByIdAsync(
        Guid eventId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                "Id",
                "EventId",
                "Name",
                "Code",
                "Description",
                "RequiredOfficials",
                "RequiredBoats",
                "RequiredQualification",
                "Priority",
                "Notes",
                "CreatedAt",
                "UpdatedAt"
            FROM "EventTasks"
            WHERE "EventId" = @EventId
              AND "Id" = @TaskId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        return await connection
            .QuerySingleOrDefaultAsync<EventTask>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        EventId = eventId,
                        TaskId = taskId
                    },
                    cancellationToken: cancellationToken));
    }

    public async Task<EventTask> CreateAsync(
        Guid eventId,
        CreateEventTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(
            request.Name,
            request.RequiredOfficials,
            request.RequiredBoats,
            request.Priority);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await EnsureEventExistsAsync(
            connection,
            eventId,
            cancellationToken);

        const string sql = """
            INSERT INTO "EventTasks"
            (
                "Id",
                "EventId",
                "Name",
                "Code",
                "Description",
                "RequiredOfficials",
                "RequiredBoats",
                "RequiredQualification",
                "Priority",
                "Notes",
                "CreatedAt",
                "UpdatedAt"
            )
            VALUES
            (
                gen_random_uuid(),
                @EventId,
                @Name,
                @Code,
                @Description,
                @RequiredOfficials,
                @RequiredBoats,
                @RequiredQualification,
                @Priority,
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
                Name = request.Name.Trim(),
                Code = NormalizeOptionalText(request.Code),
                Description =
                    NormalizeOptionalText(request.Description),
                request.RequiredOfficials,
                request.RequiredBoats,
                RequiredQualification =
                    NormalizeOptionalText(
                        request.RequiredQualification),
                Priority = NormalizePriority(request.Priority),
                Notes = NormalizeOptionalText(request.Notes)
            },
            cancellationToken: cancellationToken);

        Guid taskId;

        try
        {
            taskId =
                await connection.ExecuteScalarAsync<Guid>(
                    command);
        }
        catch (PostgresException exception)
            when (exception.SqlState == "23505")
        {
            throw new InvalidOperationException(
                "Ezzel a névvel már létezik feladat ezen a rendezvényen.",
                exception);
        }

        return await GetRequiredByIdAsync(
            connection,
            eventId,
            taskId,
            cancellationToken);
    }

    public async Task<EventTask?> UpdateAsync(
        Guid eventId,
        Guid taskId,
        UpdateEventTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(
            request.Name,
            request.RequiredOfficials,
            request.RequiredBoats,
            request.Priority);

        const string sql = """
            UPDATE "EventTasks"
            SET
                "Name" = @Name,
                "Code" = @Code,
                "Description" = @Description,
                "RequiredOfficials" = @RequiredOfficials,
                "RequiredBoats" = @RequiredBoats,
                "RequiredQualification" = @RequiredQualification,
                "Priority" = @Priority,
                "Notes" = @Notes,
                "UpdatedAt" = now()
            WHERE "EventId" = @EventId
              AND "Id" = @TaskId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                TaskId = taskId,
                Name = request.Name.Trim(),
                Code = NormalizeOptionalText(request.Code),
                Description =
                    NormalizeOptionalText(request.Description),
                request.RequiredOfficials,
                request.RequiredBoats,
                RequiredQualification =
                    NormalizeOptionalText(
                        request.RequiredQualification),
                Priority = NormalizePriority(request.Priority),
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
                "Ezzel a névvel már létezik feladat ezen a rendezvényen.",
                exception);
        }

        if (affectedRows == 0)
        {
            return null;
        }

        return await GetRequiredByIdAsync(
            connection,
            eventId,
            taskId,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid eventId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM "EventTasks"
            WHERE "EventId" = @EventId
              AND "Id" = @TaskId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        var affectedRows =
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        EventId = eventId,
                        TaskId = taskId
                    },
                    cancellationToken: cancellationToken));

        return affectedRows > 0;
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

    private static async Task<EventTask> GetRequiredByIdAsync(
        NpgsqlConnection connection,
        Guid eventId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                "Id",
                "EventId",
                "Name",
                "Code",
                "Description",
                "RequiredOfficials",
                "RequiredBoats",
                "RequiredQualification",
                "Priority",
                "Notes",
                "CreatedAt",
                "UpdatedAt"
            FROM "EventTasks"
            WHERE "EventId" = @EventId
              AND "Id" = @TaskId;
            """;

        var task =
            await connection
                .QuerySingleOrDefaultAsync<EventTask>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            EventId = eventId,
                            TaskId = taskId
                        },
                        cancellationToken:
                            cancellationToken));

        return task
            ?? throw new InvalidOperationException(
                "A feladat létrejött, de nem sikerült visszaolvasni.");
    }

    private static void ValidateRequest(
        string? name,
        int requiredOfficials,
        int requiredBoats,
        string? priority)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A feladat neve kötelező.");
        }

        if (name.Trim().Length > 200)
        {
            throw new ArgumentException(
                "A feladat neve legfeljebb 200 karakter lehet.");
        }

        if (requiredOfficials < 0)
        {
            throw new ArgumentException(
                "A szükséges rendezők száma nem lehet negatív.");
        }

        if (requiredBoats < 0)
        {
            throw new ArgumentException(
                "A szükséges hajók száma nem lehet negatív.");
        }

        if (
            !string.IsNullOrWhiteSpace(priority) &&
            priority.Trim().Length > 50)
        {
            throw new ArgumentException(
                "A prioritás legfeljebb 50 karakter lehet.");
        }
    }

    private static string NormalizePriority(
        string? priority)
    {
        return string.IsNullOrWhiteSpace(priority)
            ? "Normal"
            : priority.Trim();
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}