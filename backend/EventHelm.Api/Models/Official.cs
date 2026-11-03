namespace EventHelm.Api.Models;

public sealed class Official
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Role { get; set; }

    // Régi kompatibilitási mező.
    // Az új adatlapon nem jelenik meg aktív/inaktív állapotként.
    public string Status { get; set; } = "Available";

    public string? Club { get; set; }

    public string? ShirtSize { get; set; }

    public string? ProfileImageUrl { get; set; }

    public string? OrganizerQualification { get; set; }

    public string? JudgeQualification { get; set; }

    public string? MeasurerQualification { get; set; }

    public bool IsCompetitionAdministrator { get; set; }

    public bool IsResultScorer { get; set; }

    public DateTime? QualificationValidUntil { get; set; }

    public string? QualificationDocumentUrl { get; set; }

    public string? BoatLicenseType { get; set; }

    public DateTime? BoatLicenseValidUntil { get; set; }

    public string? BoatLicenseDocumentUrl { get; set; }

    public string? FoodAllergies { get; set; }

    public string? DrugAllergies { get; set; }

    public string? HealthNotes { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? EmergencyContactRelation { get; set; }

    public string? Languages { get; set; }

    public string? OtherDocumentUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool CanDriveMotorboat =>
        !string.IsNullOrWhiteSpace(BoatLicenseType) &&
        (
            !BoatLicenseValidUntil.HasValue ||
            BoatLicenseValidUntil.Value.Date >= DateTime.UtcNow.Date
        );
}

public sealed class OfficialCompetitionHistoryItem
{
    public Guid EventId { get; set; }

    public string EventName { get; set; } = "";

    public DateTime? StartDate { get; set; }

    public string? Location { get; set; }

    public string Role { get; set; } = "";

    public string AttendanceStatus { get; set; } = "Planned";
}

public sealed class OfficialRoleStatistic
{
    public string Role { get; set; } = "";

    public int Count { get; set; }
}

public sealed class OfficialProfileResponse
{
    public Official Official { get; set; } = new();

    public int TotalCompetitions { get; set; }

    public int CurrentYearCompetitions { get; set; }

    public IReadOnlyList<OfficialRoleStatistic> RoleStatistics { get; set; } =
        [];

    public IReadOnlyList<OfficialCompetitionHistoryItem> CompetitionHistory
    {
        get;
        set;
    } = [];
}