using Dapper;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public sealed class EventOfficialAssignmentService : IEventOfficialAssignmentService
{
    private readonly IConfiguration _configuration;

    public EventOfficialAssignmentService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("A DefaultConnection nincs beállítva.");

        return new NpgsqlConnection(connectionString);
    }

    public async Task<IReadOnlyList<EventOfficialAssignmentDetails>> GetByEventAsync(Guid eventId)
    {
        await using var connection = CreateConnection();

        var items = await connection.QueryAsync<EventOfficialAssignmentDetails>(
            SelectDetailsSql +
            """
            WHERE a."EventId" = @EventId
            ORDER BY
                COALESCE(a."GroupName", ''),
                a."AssignmentOrder",
                a."Role",
                o."Name";
            """,
            new { EventId = eventId });

        return items.ToList();
    }

    public async Task<EventOfficialAssignmentDetails?> GetByIdAsync(
        Guid eventId,
        Guid assignmentId)
    {
        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<EventOfficialAssignmentDetails>(
            SelectDetailsSql +
            """
            WHERE a."EventId" = @EventId
              AND a."Id" = @AssignmentId;
            """,
            new { EventId = eventId, AssignmentId = assignmentId });
    }

    public async Task<EventOfficialAssignmentDetails> CreateAsync(
        Guid eventId,
        SaveEventOfficialAssignmentRequest request)
    {
        ValidateRequest(request);

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await EnsureReferencesExistAsync(connection, transaction, eventId, request);
        await EnsureNoConflictsAsync(connection, transaction, eventId, request, null);

        var assignmentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO "EventOfficialAssignments"
            (
                "Id", "EventId", "OfficialId", "Role",
                "GroupName", "SubgroupName", "Task", "AssignmentOrder",
                "TrunkRadioNumber", "VhfRadioNumber", "BoatId",
                "AttendanceStatus", "Notes", "Source", "CreatedAt", "UpdatedAt"
            )
            VALUES
            (
                @Id, @EventId, @OfficialId, @Role,
                @GroupName, @SubgroupName, @Task, @AssignmentOrder,
                @TrunkRadioNumber, @VhfRadioNumber, @BoatId,
                @AttendanceStatus, @Notes, 'manual', @CreatedAt, @UpdatedAt
            );
            """,
            CreateParameters(assignmentId, eventId, request, now),
            transaction);

        await transaction.CommitAsync();

        return await GetByIdAsync(eventId, assignmentId)
            ?? throw new InvalidOperationException("A létrehozott hozzárendelés nem olvasható vissza.");
    }

    public async Task<EventOfficialAssignmentDetails?> UpdateAsync(
        Guid eventId,
        Guid assignmentId,
        SaveEventOfficialAssignmentRequest request)
    {
        ValidateRequest(request);

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var exists = await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1 FROM "EventOfficialAssignments"
                WHERE "Id" = @AssignmentId AND "EventId" = @EventId
            );
            """,
            new { AssignmentId = assignmentId, EventId = eventId },
            transaction);

        if (!exists)
        {
            await transaction.RollbackAsync();
            return null;
        }

        await EnsureReferencesExistAsync(connection, transaction, eventId, request);
        await EnsureNoConflictsAsync(connection, transaction, eventId, request, assignmentId);

        await connection.ExecuteAsync(
            """
            UPDATE "EventOfficialAssignments"
            SET
                "OfficialId" = @OfficialId,
                "Role" = @Role,
                "GroupName" = @GroupName,
                "SubgroupName" = @SubgroupName,
                "Task" = @Task,
                "AssignmentOrder" = @AssignmentOrder,
                "TrunkRadioNumber" = @TrunkRadioNumber,
                "VhfRadioNumber" = @VhfRadioNumber,
                "BoatId" = @BoatId,
                "AttendanceStatus" = @AttendanceStatus,
                "Notes" = @Notes,
                "UpdatedAt" = @UpdatedAt
            WHERE "Id" = @Id AND "EventId" = @EventId;
            """,
            CreateParameters(assignmentId, eventId, request, DateTime.UtcNow),
            transaction);

        await transaction.CommitAsync();
        return await GetByIdAsync(eventId, assignmentId);
    }

    public async Task<bool> DeleteAsync(Guid eventId, Guid assignmentId)
    {
        await using var connection = CreateConnection();

        var rows = await connection.ExecuteAsync(
            """
            DELETE FROM "EventOfficialAssignments"
            WHERE "Id" = @AssignmentId AND "EventId" = @EventId;
            """,
            new { AssignmentId = assignmentId, EventId = eventId });

        return rows > 0;
    }

    private const string SelectDetailsSql =
        """
        SELECT
            a."Id",
            a."EventId",
            a."OfficialId",
            o."Name" AS "OfficialName",
            o."Phone" AS "OfficialPhone",
            o."Email" AS "OfficialEmail",
            a."Role",
            a."GroupName",
            a."SubgroupName",
            a."Task",
            a."AssignmentOrder",
            a."TrunkRadioNumber",
            a."VhfRadioNumber",
            a."BoatId",
            b."Name" AS "BoatName",
            b."CallSign" AS "BoatCallSign",
            a."AttendanceStatus",
            a."Notes",
            a."Source",
            a."CreatedAt",
            a."UpdatedAt"
        FROM "EventOfficialAssignments" a
        INNER JOIN "Officials" o ON o."Id" = a."OfficialId"
        LEFT JOIN "Boats" b ON b."Id" = a."BoatId"
        """;

    private static object CreateParameters(
        Guid assignmentId,
        Guid eventId,
        SaveEventOfficialAssignmentRequest request,
        DateTime updatedAt)
    {
        return new
        {
            Id = assignmentId,
            EventId = eventId,
            request.OfficialId,
            Role = request.Role.Trim(),
            GroupName = Normalize(request.GroupName),
            SubgroupName = Normalize(request.SubgroupName),
            Task = Normalize(request.Task),
            AssignmentOrder = Math.Max(0, request.AssignmentOrder),
            TrunkRadioNumber = Normalize(request.TrunkRadioNumber),
            VhfRadioNumber = Normalize(request.VhfRadioNumber),
            request.BoatId,
            AttendanceStatus = request.AttendanceStatus.Trim(),
            Notes = Normalize(request.Notes),
            CreatedAt = updatedAt,
            UpdatedAt = updatedAt
        };
    }

    private static void ValidateRequest(SaveEventOfficialAssignmentRequest request)
    {
        if (request.OfficialId == Guid.Empty)
            throw new ArgumentException("A rendező kiválasztása kötelező.");

        if (string.IsNullOrWhiteSpace(request.Role))
            throw new ArgumentException("A versenybeli pozíció megadása kötelező.");

        if (string.IsNullOrWhiteSpace(request.AttendanceStatus))
            throw new ArgumentException("A jelenléti állapot megadása kötelező.");

        if (request.TrunkRadioNumber is not null &&
            !System.Text.RegularExpressions.Regex.IsMatch(request.TrunkRadioNumber.Trim(), @"^\d{3}$"))
            throw new ArgumentException("A trönk rádió azonosítója pontosan 3 számjegy.");

        if (request.VhfRadioNumber is not null &&
            !System.Text.RegularExpressions.Regex.IsMatch(request.VhfRadioNumber.Trim(), @"^\d{4}$"))
            throw new ArgumentException("A VHF rádió azonosítója pontosan 4 számjegy.");
    }

    private static async Task EnsureReferencesExistAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid eventId,
        SaveEventOfficialAssignmentRequest request)
    {
        var eventExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM \"Events\" WHERE \"Id\" = @Id);",
            new { Id = eventId }, transaction);

        if (!eventExists)
            throw new InvalidOperationException("A rendezvény nem található.");

        var officialExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM \"Officials\" WHERE \"Id\" = @Id);",
            new { Id = request.OfficialId }, transaction);

        if (!officialExists)
            throw new InvalidOperationException("A rendező nem található.");

        if (request.BoatId.HasValue)
        {
            var boatExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM \"Boats\" WHERE \"Id\" = @Id);",
                new { Id = request.BoatId.Value }, transaction);

            if (!boatExists)
                throw new InvalidOperationException("A motorcsónak nem található.");
        }
    }

    private static async Task EnsureNoConflictsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid eventId,
        SaveEventOfficialAssignmentRequest request,
        Guid? exceptAssignmentId)
    {
        var conflict = await connection.QueryFirstOrDefaultAsync<string>(
            """
            SELECT CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM "EventOfficialAssignments"
                    WHERE "EventId" = @EventId
                      AND "OfficialId" = @OfficialId
                      AND LOWER(COALESCE("GroupName", '')) = LOWER(COALESCE(@GroupName, ''))
                      AND LOWER(COALESCE("SubgroupName", '')) = LOWER(COALESCE(@SubgroupName, ''))
                      AND LOWER("Role") = LOWER(@Role)
                      AND LOWER(COALESCE("Task", '')) = LOWER(COALESCE(@Task, ''))
                      AND (@ExceptAssignmentId IS NULL OR "Id" <> @ExceptAssignmentId)
                )
                THEN 'Ez a rendező ugyanilyen beosztással már szerepel az eseményen.'

                WHEN @TrunkRadioNumber IS NOT NULL AND EXISTS
                (
                    SELECT 1 FROM "EventOfficialAssignments"
                    WHERE "EventId" = @EventId
                      AND "TrunkRadioNumber" = @TrunkRadioNumber
                      AND (@ExceptAssignmentId IS NULL OR "Id" <> @ExceptAssignmentId)
                )
                THEN 'Ez a trönk rádió ezen a versenyen már ki van osztva.'

                WHEN @VhfRadioNumber IS NOT NULL AND EXISTS
                (
                    SELECT 1 FROM "EventOfficialAssignments"
                    WHERE "EventId" = @EventId
                      AND "VhfRadioNumber" = @VhfRadioNumber
                      AND (@ExceptAssignmentId IS NULL OR "Id" <> @ExceptAssignmentId)
                )
                THEN 'Ez a VHF rádió ezen a versenyen már ki van osztva.'

                ELSE NULL
            END;
            """,
            new
            {
                EventId = eventId,
                request.OfficialId,
                GroupName = Normalize(request.GroupName),
                SubgroupName = Normalize(request.SubgroupName),
                Role = request.Role.Trim(),
                Task = Normalize(request.Task),
                TrunkRadioNumber = Normalize(request.TrunkRadioNumber),
                VhfRadioNumber = Normalize(request.VhfRadioNumber),
                ExceptAssignmentId = exceptAssignmentId
            },
            transaction);

        if (!string.IsNullOrWhiteSpace(conflict))
            throw new InvalidOperationException(conflict);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
