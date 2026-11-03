using Dapper;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public sealed class EventAssignmentService
{
    private readonly string _connectionString;

    public EventAssignmentService(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A DefaultConnection adatbázis-kapcsolat nincs beállítva.");
    }

    public async Task<IReadOnlyList<EventAssignment>> GetAllAsync(
        Guid eventId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
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
                a."UpdatedAt"
            FROM "EventAssignments" a
            WHERE a."EventId" = @EventId
              AND (
                    @From IS NULL
                    OR a."EndAt" > @From
                  )
              AND (
                    @To IS NULL
                    OR a."StartAt" < @To
                  )
            ORDER BY
                a."StartAt",
                a."EndAt",
                a."Title";
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                From = from,
                To = to
            },
            cancellationToken: cancellationToken);

        var assignments =
            await connection.QueryAsync<EventAssignment>(command);

        return assignments.ToList();
    }

    public async Task<EventAssignment?> GetByIdAsync(
        Guid eventId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
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
                a."UpdatedAt"
            FROM "EventAssignments" a
            WHERE a."EventId" = @EventId
              AND a."Id" = @AssignmentId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        var command = new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                AssignmentId = assignmentId
            },
            cancellationToken: cancellationToken);

        return await connection
            .QuerySingleOrDefaultAsync<EventAssignment>(command);
    }

    public async Task<EventAssignment> CreateAsync(
        Guid eventId,
        CreateEventAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateBasicRequest(
            request.OfficialId,
            request.Title,
            request.StartAt,
            request.EndAt,
            request.Status);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await ValidateReferencesAsync(
                connection,
                transaction,
                eventId,
                request.OfficialId,
                request.EventTaskId,
                request.EventBoatId,
                request.EventAreaId,
                cancellationToken);

            var validation =
                await ValidateAssignmentInternalAsync(
                    connection,
                    transaction,
                    eventId,
                    new ValidateEventAssignmentRequest
                    {
                        OfficialId = request.OfficialId,
                        EventTaskId = request.EventTaskId,
                        EventBoatId = request.EventBoatId,
                        AssignmentGroupId =
                            request.AssignmentGroupId,
                        StartAt = request.StartAt,
                        EndAt = request.EndAt
                    },
                    cancellationToken);

            if (!validation.CanAssign)
            {
                throw new AssignmentValidationException(
                    validation);
            }

            const string sql = """
                INSERT INTO "EventAssignments"
                (
                    "Id",
                    "EventId",
                    "OfficialId",
                    "EventTaskId",
                    "EventBoatId",
                    "EventAreaId",
                    "AssignmentGroupId",
                    "Title",
                    "Role",
                    "StartAt",
                    "EndAt",
                    "Status",
                    "Notes",
                    "IsLocked",
                    "CreatedAt",
                    "UpdatedAt"
                )
                VALUES
                (
                    gen_random_uuid(),
                    @EventId,
                    @OfficialId,
                    @EventTaskId,
                    @EventBoatId,
                    @EventAreaId,
                    @AssignmentGroupId,
                    @Title,
                    @Role,
                    @StartAt,
                    @EndAt,
                    @Status,
                    @Notes,
                    @IsLocked,
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
                    request.OfficialId,
                    request.EventTaskId,
                    request.EventBoatId,
                    request.EventAreaId,
                    request.AssignmentGroupId,
                    Title = request.Title.Trim(),
                    Role = NormalizeOptionalText(request.Role),
                    request.StartAt,
                    request.EndAt,
                    Status = NormalizeStatus(request.Status),
                    Notes = NormalizeOptionalText(request.Notes),
                    request.IsLocked
                },
                transaction,
                cancellationToken: cancellationToken);

            var assignmentId =
                await connection.ExecuteScalarAsync<Guid>(command);

            await transaction.CommitAsync(cancellationToken);

            return await GetRequiredByIdAsync(
                eventId,
                assignmentId,
                cancellationToken);
        }
        catch (PostgresException exception)
            when (exception.SqlState == "23P01")
        {
            await transaction.RollbackAsync(cancellationToken);

            throw new InvalidOperationException(
                "A rendezőnek már van másik beosztása ebben az időszakban.",
                exception);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<EventAssignment?> UpdateAsync(
        Guid eventId,
        Guid assignmentId,
        UpdateEventAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateBasicRequest(
            request.OfficialId,
            request.Title,
            request.StartAt,
            request.EndAt,
            request.Status);

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var existing =
                await GetByIdInternalAsync(
                    connection,
                    transaction,
                    eventId,
                    assignmentId,
                    cancellationToken);

            if (existing is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (existing.IsLocked)
            {
                throw new InvalidOperationException(
                    "A beosztás zárolva van, ezért nem módosítható.");
            }

            await ValidateReferencesAsync(
                connection,
                transaction,
                eventId,
                request.OfficialId,
                request.EventTaskId,
                request.EventBoatId,
                request.EventAreaId,
                cancellationToken);

            var validation =
                await ValidateAssignmentInternalAsync(
                    connection,
                    transaction,
                    eventId,
                    new ValidateEventAssignmentRequest
                    {
                        OfficialId = request.OfficialId,
                        EventTaskId = request.EventTaskId,
                        EventBoatId = request.EventBoatId,
                        AssignmentGroupId =
                            existing.AssignmentGroupId,
                        ExcludedAssignmentId = assignmentId,
                        StartAt = request.StartAt,
                        EndAt = request.EndAt
                    },
                    cancellationToken);

            if (!validation.CanAssign)
            {
                throw new AssignmentValidationException(
                    validation);
            }

            const string sql = """
                UPDATE "EventAssignments"
                SET
                    "OfficialId" = @OfficialId,
                    "EventTaskId" = @EventTaskId,
                    "EventBoatId" = @EventBoatId,
                    "EventAreaId" = @EventAreaId,
                    "Title" = @Title,
                    "Role" = @Role,
                    "StartAt" = @StartAt,
                    "EndAt" = @EndAt,
                    "Status" = @Status,
                    "Notes" = @Notes,
                    "IsLocked" = @IsLocked,
                    "UpdatedAt" = now()
                WHERE "EventId" = @EventId
                  AND "Id" = @AssignmentId;
                """;

            var command = new CommandDefinition(
                sql,
                new
                {
                    EventId = eventId,
                    AssignmentId = assignmentId,
                    request.OfficialId,
                    request.EventTaskId,
                    request.EventBoatId,
                    request.EventAreaId,
                    Title = request.Title.Trim(),
                    Role = NormalizeOptionalText(request.Role),
                    request.StartAt,
                    request.EndAt,
                    Status = NormalizeStatus(request.Status),
                    Notes = NormalizeOptionalText(request.Notes),
                    request.IsLocked
                },
                transaction,
                cancellationToken: cancellationToken);

            var affectedRows =
                await connection.ExecuteAsync(command);

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);

            return await GetRequiredByIdAsync(
                eventId,
                assignmentId,
                cancellationToken);
        }
        catch (PostgresException exception)
            when (exception.SqlState == "23P01")
        {
            await transaction.RollbackAsync(cancellationToken);

            throw new InvalidOperationException(
                "A rendezőnek már van másik beosztása ebben az időszakban.",
                exception);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        Guid eventId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        const string selectSql = """
            SELECT
                "IsLocked"
            FROM "EventAssignments"
            WHERE "EventId" = @EventId
              AND "Id" = @AssignmentId;
            """;

        const string deleteSql = """
            DELETE FROM "EventAssignments"
            WHERE "EventId" = @EventId
              AND "Id" = @AssignmentId;
            """;

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var isLocked =
            await connection.QuerySingleOrDefaultAsync<bool?>(
                new CommandDefinition(
                    selectSql,
                    new
                    {
                        EventId = eventId,
                        AssignmentId = assignmentId
                    },
                    cancellationToken: cancellationToken));

        if (!isLocked.HasValue)
        {
            return false;
        }

        if (isLocked.Value)
        {
            throw new InvalidOperationException(
                "A beosztás zárolva van, ezért nem törölhető.");
        }

        var affectedRows =
            await connection.ExecuteAsync(
                new CommandDefinition(
                    deleteSql,
                    new
                    {
                        EventId = eventId,
                        AssignmentId = assignmentId
                    },
                    cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    public async Task<AssignmentValidationResult> ValidateAsync(
        Guid eventId,
        ValidateEventAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OfficialId == Guid.Empty)
        {
            throw new ArgumentException(
                "A rendező azonosítója kötelező.");
        }

        if (request.EndAt <= request.StartAt)
        {
            throw new ArgumentException(
                "A beosztás befejezési idejének későbbinek kell lennie a kezdésnél.");
        }

        await using var connection =
            new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        return await ValidateAssignmentInternalAsync(
            connection,
            null,
            eventId,
            request,
            cancellationToken);
    }

    private static async Task<AssignmentValidationResult>
        ValidateAssignmentInternalAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction? transaction,
            Guid eventId,
            ValidateEventAssignmentRequest request,
            CancellationToken cancellationToken)
    {
        var errors =
            new List<AssignmentValidationMessage>();

        var warnings =
            new List<AssignmentValidationMessage>();

        var rules =
            await GetPlanningRulesAsync(
                connection,
                transaction,
                eventId,
                cancellationToken);

        var overlapExists =
            await HasOfficialOverlapAsync(
                connection,
                transaction,
                eventId,
                request.OfficialId,
                request.StartAt,
                request.EndAt,
                request.ExcludedAssignmentId,
                cancellationToken);

        if (overlapExists)
        {
            errors.Add(new AssignmentValidationMessage
            {
                Code = "OFFICIAL_TIME_CONFLICT",
                Message =
                    "A rendező már szerepel másik beosztásban ebben az időszakban."
            });
        }

        if (request.EventBoatId.HasValue)
        {
            var boatConflictExists =
                await HasBoatConflictAsync(
                    connection,
                    transaction,
                    eventId,
                    request.EventBoatId.Value,
                    request.AssignmentGroupId,
                    request.StartAt,
                    request.EndAt,
                    request.ExcludedAssignmentId,
                    cancellationToken);

            if (boatConflictExists)
            {
                errors.Add(new AssignmentValidationMessage
                {
                    Code = "BOAT_TIME_CONFLICT",
                    Message =
                        "A hajó ebben az időszakban már másik feladathoz van beosztva."
                });
            }
        }

        var availabilityExists =
            await HasAvailabilityRecordsAsync(
                connection,
                transaction,
                eventId,
                request.OfficialId,
                cancellationToken);

        if (availabilityExists)
        {
            var isAvailable =
                await IsInsideAvailabilityAsync(
                    connection,
                    transaction,
                    eventId,
                    request.OfficialId,
                    request.StartAt,
                    request.EndAt,
                    cancellationToken);

            if (!isAvailable)
            {
                var message =
                    new AssignmentValidationMessage
                    {
                        Code = "OUTSIDE_AVAILABILITY",
                        Message =
                            "A beosztás kívül esik a rendező megadott elérhetőségi idején."
                    };

                if (rules.BlockOutsideAvailability)
                {
                    errors.Add(message);
                }
                else
                {
                    warnings.Add(message);
                }
            }
        }

        var workload =
            await GetWorkloadSummaryAsync(
                connection,
                transaction,
                eventId,
                request.OfficialId,
                request.StartAt,
                request.EndAt,
                request.ExcludedAssignmentId,
                cancellationToken);

        if (
            rules.MaximumDailyWorkMinutes.HasValue &&
            workload.TotalPlannedMinutes >
            rules.MaximumDailyWorkMinutes.Value)
        {
            warnings.Add(new AssignmentValidationMessage
            {
                Code = "MAXIMUM_DAILY_WORK_EXCEEDED",
                Message =
                    $"A napi tervezett munkaidő " +
                    $"{workload.TotalPlannedMinutes} perc lenne, " +
                    $"ami meghaladja a beállított " +
                    $"{rules.MaximumDailyWorkMinutes.Value} percet."
            });
        }

        if (
            rules.MinimumRestMinutes > 0 &&
            workload.RestBeforeMinutes.HasValue &&
            workload.RestBeforeMinutes.Value <
            rules.MinimumRestMinutes)
        {
            warnings.Add(new AssignmentValidationMessage
            {
                Code = "INSUFFICIENT_REST_BEFORE",
                Message =
                    $"Az előző feladat után csak " +
                    $"{workload.RestBeforeMinutes.Value} perc pihenő marad. " +
                    $"Az elvárt minimum {rules.MinimumRestMinutes} perc."
            });
        }

        if (
            rules.MinimumRestMinutes > 0 &&
            workload.RestAfterMinutes.HasValue &&
            workload.RestAfterMinutes.Value <
            rules.MinimumRestMinutes)
        {
            warnings.Add(new AssignmentValidationMessage
            {
                Code = "INSUFFICIENT_REST_AFTER",
                Message =
                    $"A következő feladat előtt csak " +
                    $"{workload.RestAfterMinutes.Value} perc pihenő marad. " +
                    $"Az elvárt minimum {rules.MinimumRestMinutes} perc."
            });
        }

        return new AssignmentValidationResult
        {
            CanAssign = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            Workload = workload
        };
    }

    private static async Task ValidateReferencesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid eventId,
        Guid officialId,
        Guid? eventTaskId,
        Guid? eventBoatId,
        Guid? eventAreaId,
        CancellationToken cancellationToken)
    {
        const string eventSql = """
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
                    eventSql,
                    new
                    {
                        EventId = eventId
                    },
                    transaction,
                    cancellationToken: cancellationToken));

        if (!eventExists)
        {
            throw new KeyNotFoundException(
                "A megadott rendezvény nem található.");
        }

        const string officialSql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM "EventOfficials"
                WHERE "EventId" = @EventId
                  AND "OfficialId" = @OfficialId
            );
            """;

        var officialExists =
            await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    officialSql,
                    new
                    {
                        EventId = eventId,
                        OfficialId = officialId
                    },
                    transaction,
                    cancellationToken: cancellationToken));

        if (!officialExists)
        {
            throw new InvalidOperationException(
                "A rendező nincs hozzáadva ehhez a rendezvényhez.");
        }

        if (eventTaskId.HasValue)
        {
            const string taskSql = """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM "EventTasks"
                    WHERE "EventId" = @EventId
                      AND "Id" = @EventTaskId
                );
                """;

            var taskExists =
                await connection.ExecuteScalarAsync<bool>(
                    new CommandDefinition(
                        taskSql,
                        new
                        {
                            EventId = eventId,
                            EventTaskId = eventTaskId.Value
                        },
                        transaction,
                        cancellationToken: cancellationToken));

            if (!taskExists)
            {
                throw new InvalidOperationException(
                    "A kiválasztott feladat nem tartozik ehhez a rendezvényhez.");
            }
        }

        if (eventBoatId.HasValue)
        {
            const string boatSql = """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM "EventBoats"
                    WHERE "EventId" = @EventId
                      AND "Id" = @EventBoatId
                );
                """;

            var boatExists =
                await connection.ExecuteScalarAsync<bool>(
                    new CommandDefinition(
                        boatSql,
                        new
                        {
                            EventId = eventId,
                            EventBoatId = eventBoatId.Value
                        },
                        transaction,
                        cancellationToken: cancellationToken));

            if (!boatExists)
            {
                throw new InvalidOperationException(
                    "A kiválasztott hajó nem tartozik ehhez a rendezvényhez.");
            }
        }

        if (eventAreaId.HasValue)
        {
            const string areaSql = """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM "EventAreas"
                    WHERE "EventId" = @EventId
                      AND "Id" = @EventAreaId
                );
                """;

            var areaExists =
                await connection.ExecuteScalarAsync<bool>(
                    new CommandDefinition(
                        areaSql,
                        new
                        {
                            EventId = eventId,
                            EventAreaId = eventAreaId.Value
                        },
                        transaction,
                        cancellationToken: cancellationToken));

            if (!areaExists)
            {
                throw new InvalidOperationException(
                    "A kiválasztott terület nem tartozik ehhez a rendezvényhez.");
            }
        }
    }

    private static async Task<bool> HasOfficialOverlapAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid eventId,
        Guid officialId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Guid? excludedAssignmentId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM "EventAssignments"
                WHERE "EventId" = @EventId
                  AND "OfficialId" = @OfficialId
                  AND "Status" <> 'Cancelled'
                  AND (
                        @ExcludedAssignmentId IS NULL
                        OR "Id" <> @ExcludedAssignmentId
                      )
                  AND "StartAt" < @EndAt
                  AND "EndAt" > @StartAt
            );
            """;

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new
                {
                    EventId = eventId,
                    OfficialId = officialId,
                    StartAt = startAt,
                    EndAt = endAt,
                    ExcludedAssignmentId = excludedAssignmentId
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private static async Task<bool> HasBoatConflictAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid eventId,
        Guid eventBoatId,
        Guid? assignmentGroupId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Guid? excludedAssignmentId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM "EventAssignments"
                WHERE "EventId" = @EventId
                  AND "EventBoatId" = @EventBoatId
                  AND "Status" <> 'Cancelled'
                  AND (
                        @ExcludedAssignmentId IS NULL
                        OR "Id" <> @ExcludedAssignmentId
                      )
                  AND "StartAt" < @EndAt
                  AND "EndAt" > @StartAt
                  AND (
                        @AssignmentGroupId IS NULL
                        OR "AssignmentGroupId" IS NULL
                        OR "AssignmentGroupId" <> @AssignmentGroupId
                      )
            );
            """;

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new
                {
                    EventId = eventId,
                    EventBoatId = eventBoatId,
                    AssignmentGroupId = assignmentGroupId,
                    StartAt = startAt,
                    EndAt = endAt,
                    ExcludedAssignmentId = excludedAssignmentId
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private static async Task<bool> HasAvailabilityRecordsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid eventId,
        Guid officialId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM "OfficialAvailability"
                WHERE "EventId" = @EventId
                  AND "OfficialId" = @OfficialId
            );
            """;

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new
                {
                    EventId = eventId,
                    OfficialId = officialId
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private static async Task<bool> IsInsideAvailabilityAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid eventId,
        Guid officialId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM "OfficialAvailability"
                WHERE "EventId" = @EventId
                  AND "OfficialId" = @OfficialId
                  AND "AvailableFrom" <= @StartAt
                  AND "AvailableTo" >= @EndAt
            );
            """;

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new
                {
                    EventId = eventId,
                    OfficialId = officialId,
                    StartAt = startAt,
                    EndAt = endAt
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private static async Task<AssignmentWorkloadSummary>
        GetWorkloadSummaryAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction? transaction,
            Guid eventId,
            Guid officialId,
            DateTimeOffset startAt,
            DateTimeOffset endAt,
            Guid? excludedAssignmentId,
            CancellationToken cancellationToken)
    {
        var dayStart =
            new DateTimeOffset(
                startAt.Year,
                startAt.Month,
                startAt.Day,
                0,
                0,
                0,
                startAt.Offset);

        var dayEnd = dayStart.AddDays(1);

        const string dailySql = """
            SELECT COALESCE
            (
                SUM
                (
                    EXTRACT
                    (
                        EPOCH FROM
                        (
                            LEAST("EndAt", @DayEnd)
                            -
                            GREATEST("StartAt", @DayStart)
                        )
                    ) / 60
                ),
                0
            )
            FROM "EventAssignments"
            WHERE "EventId" = @EventId
              AND "OfficialId" = @OfficialId
              AND "Status" <> 'Cancelled'
              AND (
                    @ExcludedAssignmentId IS NULL
                    OR "Id" <> @ExcludedAssignmentId
                  )
              AND "StartAt" < @DayEnd
              AND "EndAt" > @DayStart;
            """;

        var existingMinutesDecimal =
            await connection.ExecuteScalarAsync<decimal>(
                new CommandDefinition(
                    dailySql,
                    new
                    {
                        EventId = eventId,
                        OfficialId = officialId,
                        DayStart = dayStart,
                        DayEnd = dayEnd,
                        ExcludedAssignmentId = excludedAssignmentId
                    },
                    transaction,
                    cancellationToken: cancellationToken));

        const string previousSql = """
            SELECT "EndAt"
            FROM "EventAssignments"
            WHERE "EventId" = @EventId
              AND "OfficialId" = @OfficialId
              AND "Status" <> 'Cancelled'
              AND (
                    @ExcludedAssignmentId IS NULL
                    OR "Id" <> @ExcludedAssignmentId
                  )
              AND "EndAt" <= @StartAt
            ORDER BY "EndAt" DESC
            LIMIT 1;
            """;

        var previousEnd =
            await connection.QuerySingleOrDefaultAsync<DateTimeOffset?>(
                new CommandDefinition(
                    previousSql,
                    new
                    {
                        EventId = eventId,
                        OfficialId = officialId,
                        StartAt = startAt,
                        ExcludedAssignmentId = excludedAssignmentId
                    },
                    transaction,
                    cancellationToken: cancellationToken));

        const string nextSql = """
            SELECT "StartAt"
            FROM "EventAssignments"
            WHERE "EventId" = @EventId
              AND "OfficialId" = @OfficialId
              AND "Status" <> 'Cancelled'
              AND (
                    @ExcludedAssignmentId IS NULL
                    OR "Id" <> @ExcludedAssignmentId
                  )
              AND "StartAt" >= @EndAt
            ORDER BY "StartAt"
            LIMIT 1;
            """;

        var nextStart =
            await connection.QuerySingleOrDefaultAsync<DateTimeOffset?>(
                new CommandDefinition(
                    nextSql,
                    new
                    {
                        EventId = eventId,
                        OfficialId = officialId,
                        EndAt = endAt,
                        ExcludedAssignmentId = excludedAssignmentId
                    },
                    transaction,
                    cancellationToken: cancellationToken));

        var existingMinutes =
            (int)Math.Round(existingMinutesDecimal);

        var newAssignmentMinutes =
            (int)Math.Round((endAt - startAt).TotalMinutes);

        return new AssignmentWorkloadSummary
        {
            OfficialId = officialId,
            ExistingPlannedMinutes = existingMinutes,
            NewAssignmentMinutes = newAssignmentMinutes,
            TotalPlannedMinutes =
                existingMinutes + newAssignmentMinutes,
            RestBeforeMinutes =
                previousEnd.HasValue
                    ? Math.Max(
                        0,
                        (int)Math.Round(
                            (startAt - previousEnd.Value)
                            .TotalMinutes))
                    : null,
            RestAfterMinutes =
                nextStart.HasValue
                    ? Math.Max(
                        0,
                        (int)Math.Round(
                            (nextStart.Value - endAt)
                            .TotalMinutes))
                    : null
        };
    }

    private static async Task<EventPlanningRulesData>
        GetPlanningRulesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction? transaction,
            Guid eventId,
            CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                "MaximumDailyWorkMinutes",
                "MinimumRestMinutes",
                "MaximumContinuousWorkMinutes",
                "BlockExpiredQualifications",
                "BlockMissingQualifications",
                "BlockOutsideAvailability"
            FROM "EventPlanningRules"
            WHERE "EventId" = @EventId;
            """;

        var rules =
            await connection
                .QuerySingleOrDefaultAsync<EventPlanningRulesData>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            EventId = eventId
                        },
                        transaction,
                        cancellationToken: cancellationToken));

        return rules ?? new EventPlanningRulesData
        {
            MinimumRestMinutes = 0,
            BlockExpiredQualifications = true,
            BlockMissingQualifications = true,
            BlockOutsideAvailability = true
        };
    }

    private static async Task<EventAssignment?>
        GetByIdInternalAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Guid eventId,
            Guid assignmentId,
            CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                "Id",
                "EventId",
                "OfficialId",
                "EventTaskId",
                "EventBoatId",
                "EventAreaId",
                "AssignmentGroupId",
                "Title",
                "Role",
                "StartAt",
                "EndAt",
                "Status",
                "Notes",
                "IsLocked",
                "ActualStartAt",
                "ActualEndAt",
                "CreatedAt",
                "UpdatedAt"
            FROM "EventAssignments"
            WHERE "EventId" = @EventId
              AND "Id" = @AssignmentId;
            """;

        return await connection
            .QuerySingleOrDefaultAsync<EventAssignment>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        EventId = eventId,
                        AssignmentId = assignmentId
                    },
                    transaction,
                    cancellationToken: cancellationToken));
    }

    private async Task<EventAssignment> GetRequiredByIdAsync(
        Guid eventId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var assignment =
            await GetByIdAsync(
                eventId,
                assignmentId,
                cancellationToken);

        return assignment
            ?? throw new InvalidOperationException(
                "A beosztás létrejött, de nem sikerült visszaolvasni.");
    }

    private static void ValidateBasicRequest(
        Guid officialId,
        string? title,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        string? status)
    {
        if (officialId == Guid.Empty)
        {
            throw new ArgumentException(
                "A rendező kiválasztása kötelező.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "A beosztás megnevezése kötelező.");
        }

        if (title.Trim().Length > 300)
        {
            throw new ArgumentException(
                "A beosztás megnevezése legfeljebb 300 karakter lehet.");
        }

        if (endAt <= startAt)
        {
            throw new ArgumentException(
                "A befejezési időpontnak későbbinek kell lennie a kezdésnél.");
        }

        var normalizedStatus =
            NormalizeStatus(status);

        var allowedStatuses =
            new[]
            {
                "Planned",
                "Confirmed",
                "InProgress",
                "Completed",
                "Cancelled"
            };

        if (!allowedStatuses.Contains(
                normalizedStatus,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Érvénytelen beosztási állapot.");
        }
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "Planned";
        }

        var trimmed = status.Trim();

        return trimmed.ToLowerInvariant() switch
        {
            "planned" => "Planned",
            "confirmed" => "Confirmed",
            "inprogress" => "InProgress",
            "completed" => "Completed",
            "cancelled" => "Cancelled",
            _ => trimmed
        };
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed class EventPlanningRulesData
    {
        public int? MaximumDailyWorkMinutes { get; set; }

        public int MinimumRestMinutes { get; set; }

        public int? MaximumContinuousWorkMinutes { get; set; }

        public bool BlockExpiredQualifications { get; set; }

        public bool BlockMissingQualifications { get; set; }

        public bool BlockOutsideAvailability { get; set; }
    }
}

public sealed class AssignmentValidationException : Exception
{
    public AssignmentValidationException(
        AssignmentValidationResult validation)
        : base(CreateMessage(validation))
    {
        Validation = validation;
    }

    public AssignmentValidationResult Validation { get; }

    private static string CreateMessage(
        AssignmentValidationResult validation)
    {
        if (validation.Errors.Count == 0)
        {
            return "A beosztás nem hozható létre.";
        }

        return string.Join(
            " ",
            validation.Errors.Select(error => error.Message));
    }
}