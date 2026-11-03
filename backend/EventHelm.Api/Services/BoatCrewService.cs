using Dapper;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public sealed class BoatCrewService : IBoatCrewService
{
    private readonly IConfiguration _configuration;

    public BoatCrewService(IConfiguration configuration)
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

    private const string SelectColumns =
        """
        a."Id",
        a."BoatId",
        a."OfficialId",
        a."Role",
        a."IsCaptain",
        a."FromDate"::timestamp AS "FromDate",
        a."ToDate"::timestamp AS "ToDate",
        a."Notes",
        a."CreatedAt",
        o."Name" AS "OfficialName",
        o."Phone" AS "OfficialPhone",
        o."Email" AS "OfficialEmail",
        o."ProfileImageUrl" AS "OfficialProfileImageUrl"
        """;

    public async Task<IReadOnlyList<BoatCrewAssignment>> GetByBoatIdAsync(
        Guid boatId)
    {
        await using var connection = CreateConnection();

        var assignments =
            await connection.QueryAsync<BoatCrewAssignment>(
                $"""
                SELECT
                    {SelectColumns}
                FROM "BoatCrewAssignments" a
                INNER JOIN "Officials" o
                    ON o."Id" = a."OfficialId"
                WHERE a."BoatId" = @BoatId
                ORDER BY
                    a."IsCaptain" DESC,
                    o."Name";
                """,
                new
                {
                    BoatId = boatId
                });

        return assignments.ToList();
    }

    public async Task<BoatCrewAssignment?> GetByIdAsync(Guid id)
    {
        await using var connection = CreateConnection();

        return await connection
            .QueryFirstOrDefaultAsync<BoatCrewAssignment>(
                $"""
                SELECT
                    {SelectColumns}
                FROM "BoatCrewAssignments" a
                INNER JOIN "Officials" o
                    ON o."Id" = a."OfficialId"
                WHERE a."Id" = @Id;
                """,
                new
                {
                    Id = id
                });
    }

    public async Task<BoatCrewAssignment> CreateAsync(
        Guid boatId,
        CreateBoatCrewAssignmentRequest request)
    {
        ApplyDefaults(request);
        Validate(request);

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            var boatExists =
                await connection.ExecuteScalarAsync<bool>(
                    """
                    SELECT EXISTS
                    (
                        SELECT 1
                        FROM "Boats"
                        WHERE "Id" = @BoatId
                    );
                    """,
                    new
                    {
                        BoatId = boatId
                    },
                    transaction);

            if (!boatExists)
            {
                throw new ArgumentException(
                    "A megadott hajó nem található.");
            }

            var officialExists =
                await connection.ExecuteScalarAsync<bool>(
                    """
                    SELECT EXISTS
                    (
                        SELECT 1
                        FROM "Officials"
                        WHERE "Id" = @OfficialId
                    );
                    """,
                    new
                    {
                        request.OfficialId
                    },
                    transaction);

            if (!officialExists)
            {
                throw new ArgumentException(
                    "A megadott rendező nem található.");
            }

            var alreadyAssigned =
                await connection.ExecuteScalarAsync<bool>(
                    """
                    SELECT EXISTS
                    (
                        SELECT 1
                        FROM "BoatCrewAssignments"
                        WHERE "BoatId" = @BoatId
                          AND "OfficialId" = @OfficialId
                    );
                    """,
                    new
                    {
                        BoatId = boatId,
                        request.OfficialId
                    },
                    transaction);

            if (alreadyAssigned)
            {
                throw new ArgumentException(
                    "Ez a rendező már hozzá van rendelve ehhez a hajóhoz.");
            }

            if (request.IsCaptain)
            {
                await RemoveCurrentCaptainAsync(
                    connection,
                    transaction,
                    boatId,
                    excludedAssignmentId: null);
            }

            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                """
                INSERT INTO "BoatCrewAssignments"
                (
                    "Id",
                    "BoatId",
                    "OfficialId",
                    "Role",
                    "IsCaptain",
                    "FromDate",
                    "ToDate",
                    "Notes",
                    "CreatedAt"
                )
                VALUES
                (
                    @Id,
                    @BoatId,
                    @OfficialId,
                    @Role,
                    @IsCaptain,
                    CAST(@FromDate AS date),
                    CAST(@ToDate AS date),
                    @Notes,
                    @CreatedAt
                );
                """,
                new
                {
                    Id = id,
                    BoatId = boatId,
                    request.OfficialId,
                    request.Role,
                    request.IsCaptain,
                    FromDate = request.FromDate?.Date,
                    ToDate = request.ToDate?.Date,
                    request.Notes,
                    CreatedAt = createdAt
                },
                transaction);

            await transaction.CommitAsync();

            return await GetByIdAsync(id)
                ?? throw new InvalidOperationException(
                    "A létrehozott személyzeti hozzárendelés nem olvasható vissza.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<BoatCrewAssignment?> UpdateAsync(
        Guid id,
        UpdateBoatCrewAssignmentRequest request)
    {
        ApplyDefaults(request);
        Validate(request);

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            var current =
                await connection
                    .QueryFirstOrDefaultAsync<BoatCrewAssignment>(
                        """
                        SELECT
                            "Id",
                            "BoatId",
                            "OfficialId",
                            "Role",
                            "IsCaptain",
                            "FromDate"::timestamp AS "FromDate",
                            "ToDate"::timestamp AS "ToDate",
                            "Notes",
                            "CreatedAt"
                        FROM "BoatCrewAssignments"
                        WHERE "Id" = @Id;
                        """,
                        new
                        {
                            Id = id
                        },
                        transaction);

            if (current is null)
            {
                await transaction.RollbackAsync();
                return null;
            }

            if (request.IsCaptain)
            {
                await RemoveCurrentCaptainAsync(
                    connection,
                    transaction,
                    current.BoatId,
                    id);
            }

            await connection.ExecuteAsync(
                """
                UPDATE "BoatCrewAssignments"
                SET
                    "Role" = @Role,
                    "IsCaptain" = @IsCaptain,
                    "FromDate" = CAST(@FromDate AS date),
                    "ToDate" = CAST(@ToDate AS date),
                    "Notes" = @Notes
                WHERE "Id" = @Id;
                """,
                new
                {
                    Id = id,
                    request.Role,
                    request.IsCaptain,
                    FromDate = request.FromDate?.Date,
                    ToDate = request.ToDate?.Date,
                    request.Notes
                },
                transaction);

            await transaction.CommitAsync();

            return await GetByIdAsync(id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = CreateConnection();

        var rowsAffected =
            await connection.ExecuteAsync(
                """
                DELETE FROM "BoatCrewAssignments"
                WHERE "Id" = @Id;
                """,
                new
                {
                    Id = id
                });

        return rowsAffected > 0;
    }

    private static async Task RemoveCurrentCaptainAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid boatId,
        Guid? excludedAssignmentId)
    {
        await connection.ExecuteAsync(
            """
            UPDATE "BoatCrewAssignments"
            SET "IsCaptain" = FALSE
            WHERE "BoatId" = @BoatId
              AND "IsCaptain" = TRUE
              AND
              (
                  @ExcludedAssignmentId IS NULL
                  OR "Id" <> @ExcludedAssignmentId
              );
            """,
            new
            {
                BoatId = boatId,
                ExcludedAssignmentId = excludedAssignmentId
            },
            transaction);
    }

    private static void ApplyDefaults(
        CreateBoatCrewAssignmentRequest request)
    {
        request.Role =
            string.IsNullOrWhiteSpace(request.Role)
                ? "Rendező"
                : request.Role.Trim();

        request.Notes = Normalize(request.Notes);
    }

    private static void ApplyDefaults(
        UpdateBoatCrewAssignmentRequest request)
    {
        request.Role =
            string.IsNullOrWhiteSpace(request.Role)
                ? "Rendező"
                : request.Role.Trim();

        request.Notes = Normalize(request.Notes);
    }

    private static void Validate(
        CreateBoatCrewAssignmentRequest request)
    {
        if (request.OfficialId == Guid.Empty)
        {
            throw new ArgumentException(
                "A rendező kiválasztása kötelező.");
        }

        ValidateDates(
            request.FromDate,
            request.ToDate);
    }

    private static void Validate(
        UpdateBoatCrewAssignmentRequest request)
    {
        ValidateDates(
            request.FromDate,
            request.ToDate);
    }

    private static void ValidateDates(
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (
            fromDate.HasValue &&
            toDate.HasValue &&
            toDate.Value.Date < fromDate.Value.Date
        )
        {
            throw new ArgumentException(
                "A befejező dátum nem lehet korábbi a kezdő dátumnál.");
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}