using Dapper;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public sealed class OfficialService : IOfficialService
{
    private readonly IConfiguration _configuration;

    public OfficialService(IConfiguration configuration)
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
        "Id",
        "Name",
        "Phone",
        "Email",
        "Role",
        "Status",
        "Club",
        "ShirtSize",
        "ProfileImageUrl",
        "OrganizerQualification",
        "JudgeQualification",
        "MeasurerQualification",
        "IsCompetitionAdministrator",
        "IsResultScorer",
        "QualificationValidUntil"::timestamp AS "QualificationValidUntil",
        "QualificationDocumentUrl",
        "BoatLicenseType",
        "BoatLicenseValidUntil"::timestamp AS "BoatLicenseValidUntil",
        "BoatLicenseDocumentUrl",
        "FoodAllergies",
        "DrugAllergies",
        "HealthNotes",
        "EmergencyContactName",
        "EmergencyContactPhone",
        "EmergencyContactRelation",
        "Languages",
        "OtherDocumentUrl",
        "CreatedAt"
        """;

    public async Task<IReadOnlyList<Official>> GetAllAsync()
    {
        await using var connection = CreateConnection();

        var officials = await connection.QueryAsync<Official>(
            $"""
            SELECT
                {SelectColumns}
            FROM "Officials"
            ORDER BY "Name";
            """);

        return officials.ToList();
    }

    public async Task<Official?> GetByIdAsync(Guid id)
    {
        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<Official>(
            $"""
            SELECT
                {SelectColumns}
            FROM "Officials"
            WHERE "Id" = @Id;
            """,
            new
            {
                Id = id
            });
    }

    public async Task<OfficialProfileResponse?> GetProfileAsync(Guid id)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var official =
            await connection.QueryFirstOrDefaultAsync<Official>(
                $"""
                SELECT
                    {SelectColumns}
                FROM "Officials"
                WHERE "Id" = @Id;
                """,
                new
                {
                    Id = id
                });

        if (official is null)
        {
            return null;
        }

        var history =
            (
                await connection.QueryAsync<
                    OfficialCompetitionHistoryItem
                >(
                    """
                    SELECT
                        e."Id" AS "EventId",
                        e."Name" AS "EventName",
                        e."StartDate",
                        e."Location",
                        a."Role",
                        a."AttendanceStatus"
                    FROM "EventOfficialAssignments" a
                    INNER JOIN "Events" e
                        ON e."Id" = a."EventId"
                    WHERE a."OfficialId" = @OfficialId
                    ORDER BY
                        e."StartDate" DESC NULLS LAST,
                        e."Name";
                    """,
                    new
                    {
                        OfficialId = id
                    })
            ).ToList();

        var roleStatistics = history
            .GroupBy(item => item.Role)
            .Select(group => new OfficialRoleStatistic
            {
                Role = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Role)
            .ToList();

        var currentYear = DateTime.UtcNow.Year;

        return new OfficialProfileResponse
        {
            Official = official,

            TotalCompetitions = history.Count,

            CurrentYearCompetitions = history.Count(item =>
                item.StartDate.HasValue &&
                item.StartDate.Value.Year == currentYear),

            RoleStatistics = roleStatistics,

            CompetitionHistory = history
        };
    }

    public async Task<Official> CreateAsync(Official official)
    {
        ApplyDefaults(official);
        Validate(official);

        await using var connection = CreateConnection();

        official.Id = Guid.NewGuid();
        official.CreatedAt = DateTime.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO "Officials"
            (
                "Id",
                "Name",
                "Phone",
                "Email",
                "Role",
                "Status",
                "Club",
                "ShirtSize",
                "ProfileImageUrl",
                "OrganizerQualification",
        "JudgeQualification",
        "MeasurerQualification",
        "IsCompetitionAdministrator",
        "IsResultScorer",
                "QualificationValidUntil",
                "QualificationDocumentUrl",
                "BoatLicenseType",
                "BoatLicenseValidUntil",
                "BoatLicenseDocumentUrl",
                "FoodAllergies",
                "DrugAllergies",
                "HealthNotes",
                "EmergencyContactName",
                "EmergencyContactPhone",
                "EmergencyContactRelation",
                "Languages",
                "OtherDocumentUrl",
                "CreatedAt"
            )
            VALUES
            (
                @Id,
                @Name,
                @Phone,
                @Email,
                @Role,
                @Status,
                @Club,
                @ShirtSize,
                @ProfileImageUrl,
                @OrganizerQualification,
                @JudgeQualification,
                @MeasurerQualification,
                @IsCompetitionAdministrator,
                @IsResultScorer,
                CAST(@QualificationValidUntil AS date),
                @QualificationDocumentUrl,
                @BoatLicenseType,
                CAST(@BoatLicenseValidUntil AS date),
                @BoatLicenseDocumentUrl,
                @FoodAllergies,
                @DrugAllergies,
                @HealthNotes,
                @EmergencyContactName,
                @EmergencyContactPhone,
                @EmergencyContactRelation,
                @Languages,
                @OtherDocumentUrl,
                @CreatedAt
            );
            """,
            CreateParameters(
                official.Id,
                official,
                official.CreatedAt));

        return official;
    }

    public async Task<Official?> UpdateAsync(
        Guid id,
        Official official)
    {
        ApplyDefaults(official);
        Validate(official);

        await using var connection = CreateConnection();

        var rowsAffected = await connection.ExecuteAsync(
            """
            UPDATE "Officials"
            SET
                "Name" = @Name,
                "Phone" = @Phone,
                "Email" = @Email,
                "Role" = @Role,
                "Status" = @Status,
                "Club" = @Club,
                "ShirtSize" = @ShirtSize,
                "ProfileImageUrl" = @ProfileImageUrl,
                "OrganizerQualification" = @OrganizerQualification,
                "JudgeQualification" = @JudgeQualification,
                "MeasurerQualification" = @MeasurerQualification,
                "IsCompetitionAdministrator" =
                    @IsCompetitionAdministrator,
                "IsResultScorer" = @IsResultScorer,
                "QualificationValidUntil" =
                    CAST(@QualificationValidUntil AS date),
                "QualificationDocumentUrl" =
                    @QualificationDocumentUrl,
                "BoatLicenseType" = @BoatLicenseType,
                "BoatLicenseValidUntil" =
                    CAST(@BoatLicenseValidUntil AS date),
                "BoatLicenseDocumentUrl" =
                    @BoatLicenseDocumentUrl,
                "FoodAllergies" = @FoodAllergies,
                "DrugAllergies" = @DrugAllergies,
                "HealthNotes" = @HealthNotes,
                "EmergencyContactName" =
                    @EmergencyContactName,
                "EmergencyContactPhone" =
                    @EmergencyContactPhone,
                "EmergencyContactRelation" =
                    @EmergencyContactRelation,
                "Languages" = @Languages,
                "OtherDocumentUrl" = @OtherDocumentUrl
            WHERE "Id" = @Id;
            """,
            CreateParameters(
                id,
                official,
                createdAt: null));

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
            DELETE FROM "Officials"
            WHERE "Id" = @Id;
            """,
            new
            {
                Id = id
            });

        return rowsAffected > 0;
    }

    private static object CreateParameters(
        Guid id,
        Official official,
        DateTime? createdAt)
    {
        return new
        {
            Id = id,

            Name = official.Name,
            Phone = official.Phone,
            Email = official.Email,
            Role = official.Role,
            Status = official.Status,
            Club = official.Club,
            ShirtSize = official.ShirtSize,
            ProfileImageUrl = official.ProfileImageUrl,

            OrganizerQualification =
                official.OrganizerQualification,

            JudgeQualification =
                official.JudgeQualification,

            MeasurerQualification =
                official.MeasurerQualification,

            IsCompetitionAdministrator =
                official.IsCompetitionAdministrator,

            IsResultScorer =
                official.IsResultScorer,

            QualificationValidUntil =
                ToDatabaseDate(
                    official.QualificationValidUntil),

            QualificationDocumentUrl =
                official.QualificationDocumentUrl,

            BoatLicenseType =
                official.BoatLicenseType,

            BoatLicenseValidUntil =
                ToDatabaseDate(
                    official.BoatLicenseValidUntil),

            BoatLicenseDocumentUrl =
                official.BoatLicenseDocumentUrl,

            FoodAllergies =
                official.FoodAllergies,

            DrugAllergies =
                official.DrugAllergies,

            HealthNotes =
                official.HealthNotes,

            EmergencyContactName =
                official.EmergencyContactName,

            EmergencyContactPhone =
                official.EmergencyContactPhone,

            EmergencyContactRelation =
                official.EmergencyContactRelation,

            Languages =
                official.Languages,

            OtherDocumentUrl =
                official.OtherDocumentUrl,

            CreatedAt = createdAt
        };
    }

    private static void ApplyDefaults(Official official)
    {
        official.Name =
            official.Name.Trim();

        official.Role =
            string.IsNullOrWhiteSpace(official.Role)
                ? "Rendező"
                : official.Role.Trim();

        official.Status =
            string.IsNullOrWhiteSpace(official.Status)
                ? "Available"
                : official.Status.Trim();

        official.Phone =
            Normalize(official.Phone);

        official.Email =
            Normalize(official.Email);

        official.Club =
            Normalize(official.Club);

        official.ShirtSize =
            Normalize(official.ShirtSize);

        official.ProfileImageUrl =
            Normalize(official.ProfileImageUrl);

        official.OrganizerQualification =
            Normalize(
                official.OrganizerQualification);

        official.JudgeQualification =
            Normalize(
                official.JudgeQualification);

        official.MeasurerQualification =
            Normalize(
                official.MeasurerQualification);

        official.QualificationDocumentUrl =
            Normalize(
                official.QualificationDocumentUrl);

        official.BoatLicenseType =
            Normalize(official.BoatLicenseType);

        official.BoatLicenseDocumentUrl =
            Normalize(
                official.BoatLicenseDocumentUrl);

        official.FoodAllergies =
            Normalize(official.FoodAllergies);

        official.DrugAllergies =
            Normalize(official.DrugAllergies);

        official.HealthNotes =
            Normalize(official.HealthNotes);

        official.EmergencyContactName =
            Normalize(
                official.EmergencyContactName);

        official.EmergencyContactPhone =
            Normalize(
                official.EmergencyContactPhone);

        official.EmergencyContactRelation =
            Normalize(
                official.EmergencyContactRelation);

        official.Languages =
            Normalize(official.Languages);

        official.OtherDocumentUrl =
            Normalize(official.OtherDocumentUrl);
    }

    private static DateTime? ToDatabaseDate(
        DateTime? value)
    {
        return value?.Date;
    }

    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void Validate(Official official)
    {
        if (string.IsNullOrWhiteSpace(
            official.Name))
        {
            throw new ArgumentException(
                "A rendező neve kötelező.");
        }

        if (
            official.BoatLicenseValidUntil.HasValue &&
            string.IsNullOrWhiteSpace(
                official.BoatLicenseType)
        )
        {
            throw new ArgumentException(
                "A hajóvezetői képesítés típusát is add meg.");
        }

        if (
            official.QualificationValidUntil.HasValue &&
            string.IsNullOrWhiteSpace(
                official.OrganizerQualification)
        )
        {
            throw new ArgumentException(
                "A rendezői minősítést is add meg.");
        }
    }
}