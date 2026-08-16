using Dapper;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public sealed class EventTimelineService
{
    private readonly string _connectionString;

    public EventTimelineService(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A DefaultConnection adatbázis-kapcsolat nincs beállítva.");
    }

    public async Task<EventTimelineResult> GetTimelineAsync(
        Guid eventId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        ValidateTimeRange(from, to);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await EnsureEventExistsAsync(
            connection,
            eventId,
            cancellationToken);

        var assignments =
            await GetAssignmentsAsync(
                connection,
                eventId,
                from,
                to,
                cancellationToken);

        var officials = BuildOfficialRows(assignments);
        var boats = BuildBoatRows(assignments);

        return new EventTimelineResult
        {
            EventId = eventId,
            From = from,
            To = to,
            Assignments = assignments,
            Officials = officials,
            Boats = boats
        };
    }

    public async Task<IReadOnlyList<OfficialTimelineRow>>
        GetOfficialsTimelineAsync(
            Guid eventId,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            CancellationToken cancellationToken = default)
    {
        ValidateTimeRange(from, to);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await EnsureEventExistsAsync(
            connection,
            eventId,
            cancellationToken);

        var assignments =
            await GetAssignmentsAsync(
                connection,
                eventId,
                from,
                to,
                cancellationToken);

        return BuildOfficialRows(assignments);
    }

    public async Task<OfficialTimelineRow?>
        GetOfficialTimelineAsync(
            Guid eventId,
            Guid officialId,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            CancellationToken cancellationToken = default)
    {
        if (officialId == Guid.Empty)
        {
            throw new ArgumentException(
                "A rendező azonosítója kötelező.");
        }

        ValidateTimeRange(from, to);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await EnsureEventExistsAsync(
            connection,
            eventId,
            cancellationToken);

        var assignments =
            await GetAssignmentsAsync(
                connection,
                eventId,
                from,
                to,
                cancellationToken,
                officialId: officialId);

        if (assignments.Count == 0)
        {
            return null;
        }

        return BuildOfficialRows(assignments).Single();
    }

    public async Task<IReadOnlyList<BoatTimelineRow>>
        GetBoatsTimelineAsync(
            Guid eventId,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            CancellationToken cancellationToken = default)
    {
        ValidateTimeRange(from, to);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await EnsureEventExistsAsync(
            connection,
            eventId,
            cancellationToken);

        var assignments =
            await GetAssignmentsAsync(
                connection,
                eventId,
                from,
                to,
                cancellationToken,
                requireBoat: true);

        return BuildBoatRows(assignments);
    }

    public async Task<BoatTimelineRow?>
        GetBoatTimelineAsync(
            Guid eventId,
            Guid eventBoatId,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            CancellationToken cancellationToken = default)
    {
        if (eventBoatId == Guid.Empty)
        {
            throw new ArgumentException(
                "A rendezvényhajó azonosítója kötelező.");
        }

        ValidateTimeRange(from, to);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await EnsureEventExistsAsync(
            connection,
            eventId,
            cancellationToken);

        var assignments =
            await GetAssignmentsAsync(
                connection,
                eventId,
                from,
                to,
                cancellationToken,
                eventBoatId: eventBoatId);

        if (assignments.Count == 0)
        {
            return null;
        }

        return BuildBoatRows(assignments).Single();
    }

    private static async Task<IReadOnlyList<EventAssignment>>
        GetAssignmentsAsync(
            NpgsqlConnection connection,
            Guid eventId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken cancellationToken,
            Guid? officialId = null,
            Guid? eventBoatId = null,
            bool requireBoat = false)
    {
        const string sql = """
            SELECT
                a."Id",
                a."EventId",
                a."OfficialId",
                a."EventTaskId",
                a."EventBoatId",
                a."EventAreaId",
                a."AssignmentGroupId",
                a."Title",
                a."Role",
                a."StartAt",
                a."EndAt",
                a."Status",
                a."Notes",
                a."IsLocked",
                a."ActualStartAt",
                a."ActualEndAt",
                a."CreatedAt",
                a."UpdatedAt",

                COALESCE(
                    NULLIF(TRIM(o."Name"), ''),
                    a."OfficialId"::text
                ) AS "OfficialName",

                NULLIF(TRIM(t."Name"), '') AS "TaskName",

                COALESCE(
                    NULLIF(TRIM(eb."DisplayName"), ''),
                    NULLIF(TRIM(eb."Code"), ''),
                    NULLIF(TRIM(b."Name"), '')
                ) AS "BoatName",

                NULLIF(TRIM(ar."Name"), '') AS "AreaName"

            FROM "EventAssignments" a

            LEFT JOIN "Officials" o
                ON o."Id" = a."OfficialId"

            LEFT JOIN "EventTasks" t
                ON t."Id" = a."EventTaskId"
               AND t."EventId" = a."EventId"

            LEFT JOIN "EventBoats" eb
                ON eb."Id" = a."EventBoatId"
               AND eb."EventId" = a."EventId"

            LEFT JOIN "Boats" b
                ON b."Id" = eb."BoatId"

            LEFT JOIN "EventAreas" ar
                ON ar."Id" = a."EventAreaId"
               AND ar."EventId" = a."EventId"

            WHERE a."EventId" = @EventId

              AND (
                    @From IS NULL
                    OR a."EndAt" > @From
                  )

              AND (
                    @To IS NULL
                    OR a."StartAt" < @To
                  )

              AND (
                    @OfficialId IS NULL
                    OR a."OfficialId" = @OfficialId
                  )

              AND (
                    @EventBoatId IS NULL
                    OR a."EventBoatId" = @EventBoatId
                  )

              AND (
                    @RequireBoat = FALSE
                    OR a."EventBoatId" IS NOT NULL
                  )

            ORDER BY
                a."StartAt",
                a."EndAt",
                a."Title",
                a."OfficialId";
            """;

        var command =
            new CommandDefinition(
                sql,
                new
                {
                    EventId = eventId,
                    From = from,
                    To = to,
                    OfficialId = officialId,
                    EventBoatId = eventBoatId,
                    RequireBoat = requireBoat
                },
                cancellationToken: cancellationToken);

        var result =
            await connection.QueryAsync<EventAssignment>(command);

        return result.ToList();
    }

    private static IReadOnlyList<OfficialTimelineRow>
        BuildOfficialRows(
            IReadOnlyList<EventAssignment> assignments)
    {
        return assignments
            .GroupBy(assignment => new
            {
                assignment.OfficialId,
                assignment.OfficialName
            })
            .Select(group =>
            {
                var orderedAssignments =
                    group
                        .OrderBy(assignment => assignment.StartAt)
                        .ThenBy(assignment => assignment.EndAt)
                        .ThenBy(assignment => assignment.Title)
                        .ToList();

                return new OfficialTimelineRow
                {
                    OfficialId = group.Key.OfficialId,
                    OfficialName =
                        string.IsNullOrWhiteSpace(
                            group.Key.OfficialName)
                            ? group.Key.OfficialId.ToString()
                            : group.Key.OfficialName,

                    PlannedMinutes =
                        orderedAssignments
                            .Where(assignment =>
                                !string.Equals(
                                    assignment.Status,
                                    "Cancelled",
                                    StringComparison.OrdinalIgnoreCase))
                            .Sum(assignment =>
                                Math.Max(
                                    0,
                                    assignment.PlannedMinutes)),

                    ActualMinutes =
                        orderedAssignments
                            .Where(assignment =>
                                !string.Equals(
                                    assignment.Status,
                                    "Cancelled",
                                    StringComparison.OrdinalIgnoreCase))
                            .Sum(assignment =>
                                Math.Max(
                                    0,
                                    assignment.ActualMinutes ?? 0)),

                    Assignments = orderedAssignments
                };
            })
            .OrderBy(row => row.OfficialName)
            .ToList();
    }

    private static IReadOnlyList<BoatTimelineRow>
        BuildBoatRows(
            IReadOnlyList<EventAssignment> assignments)
    {
        return assignments
            .Where(assignment =>
                assignment.EventBoatId.HasValue)
            .GroupBy(assignment => new
            {
                EventBoatId = assignment.EventBoatId!.Value,
                assignment.BoatName
            })
            .Select(boatGroup =>
            {
                var timelineItems =
                    boatGroup
                        .GroupBy(assignment => new
                        {
                            GroupKey =
                                assignment.AssignmentGroupId
                                ?? assignment.Id,

                            assignment.AssignmentGroupId,
                            assignment.EventBoatId,
                            assignment.BoatName,
                            assignment.EventTaskId,
                            assignment.TaskName,
                            assignment.EventAreaId,
                            assignment.AreaName,
                            assignment.Title,
                            assignment.StartAt,
                            assignment.EndAt,
                            assignment.Status,
                            assignment.Notes
                        })
                        .Select(itemGroup =>
                        {
                            var members =
                                itemGroup
                                    .OrderBy(assignment =>
                                        assignment.OfficialName)
                                    .Select(assignment =>
                                        new EventAssignmentGroupMember
                                        {
                                            AssignmentId =
                                                assignment.Id,

                                            OfficialId =
                                                assignment.OfficialId,

                                            OfficialName =
                                                string.IsNullOrWhiteSpace(
                                                    assignment
                                                        .OfficialName)
                                                    ? assignment
                                                        .OfficialId
                                                        .ToString()
                                                    : assignment
                                                        .OfficialName,

                                            Role = assignment.Role
                                        })
                                    .ToList();

                            return new
                                EventAssignmentGroupTimelineItem
                            {
                                AssignmentGroupId =
                                    itemGroup.Key
                                        .AssignmentGroupId,

                                EventBoatId =
                                    itemGroup.Key.EventBoatId,

                                BoatName =
                                    itemGroup.Key.BoatName,

                                EventTaskId =
                                    itemGroup.Key.EventTaskId,

                                TaskName =
                                    itemGroup.Key.TaskName,

                                EventAreaId =
                                    itemGroup.Key.EventAreaId,

                                AreaName =
                                    itemGroup.Key.AreaName,

                                Title =
                                    itemGroup.Key.Title,

                                StartAt =
                                    itemGroup.Key.StartAt,

                                EndAt =
                                    itemGroup.Key.EndAt,

                                Status =
                                    itemGroup.Key.Status,

                                Notes =
                                    itemGroup.Key.Notes,

                                Members = members
                            };
                        })
                        .OrderBy(item => item.StartAt)
                        .ThenBy(item => item.EndAt)
                        .ThenBy(item => item.Title)
                        .ToList();

                var plannedMinutes =
                    timelineItems
                        .Where(item =>
                            !string.Equals(
                                item.Status,
                                "Cancelled",
                                StringComparison.OrdinalIgnoreCase))
                        .Sum(item =>
                            Math.Max(
                                0,
                                (int)Math.Round(
                                    (item.EndAt - item.StartAt)
                                    .TotalMinutes)));

                return new BoatTimelineRow
                {
                    EventBoatId =
                        boatGroup.Key.EventBoatId,

                    BoatName =
                        string.IsNullOrWhiteSpace(
                            boatGroup.Key.BoatName)
                            ? boatGroup.Key.EventBoatId.ToString()
                            : boatGroup.Key.BoatName,

                    PlannedMinutes = plannedMinutes,
                    Items = timelineItems
                };
            })
            .OrderBy(row => row.BoatName)
            .ToList();
    }

    private static async Task EnsureEventExistsAsync(
        NpgsqlConnection connection,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException(
                "A rendezvény azonosítója kötelező.");
        }

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

    private static void ValidateTimeRange(
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (from.HasValue &&
            to.HasValue &&
            to.Value <= from.Value)
        {
            throw new ArgumentException(
                "A záró időpontnak későbbinek kell lennie a kezdő időpontnál.");
        }
    }
}