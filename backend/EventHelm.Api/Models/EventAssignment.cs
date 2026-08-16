namespace EventHelm.Api.Models;

public sealed class EventAssignment
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid OfficialId { get; set; }

    public Guid? EventTaskId { get; set; }

    public Guid? EventBoatId { get; set; }

    public Guid? EventAreaId { get; set; }

    public Guid? AssignmentGroupId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Role { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string Status { get; set; } = "Planned";

    public string? Notes { get; set; }

    public bool IsLocked { get; set; }

    public DateTimeOffset? ActualStartAt { get; set; }

    public DateTimeOffset? ActualEndAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // Megjelenítéshez visszaadott adatok

    public string OfficialName { get; set; } = string.Empty;

    public string? TaskName { get; set; }

    public string? BoatName { get; set; }

    public string? AreaName { get; set; }

    public int PlannedMinutes =>
        (int)Math.Round((EndAt - StartAt).TotalMinutes);

    public int? ActualMinutes =>
        ActualStartAt.HasValue && ActualEndAt.HasValue
            ? (int)Math.Round(
                (ActualEndAt.Value - ActualStartAt.Value)
                .TotalMinutes)
            : null;
}

public sealed class CreateEventAssignmentRequest
{
    public Guid OfficialId { get; set; }

    public Guid? EventTaskId { get; set; }

    public Guid? EventBoatId { get; set; }

    public Guid? EventAreaId { get; set; }

    public Guid? AssignmentGroupId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Role { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string Status { get; set; } = "Planned";

    public string? Notes { get; set; }

    public bool IsLocked { get; set; }
}

public sealed class UpdateEventAssignmentRequest
{
    public Guid OfficialId { get; set; }

    public Guid? EventTaskId { get; set; }

    public Guid? EventBoatId { get; set; }

    public Guid? EventAreaId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Role { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string Status { get; set; } = "Planned";

    public string? Notes { get; set; }

    public bool IsLocked { get; set; }
}

public sealed class CreateEventAssignmentGroupRequest
{
    public Guid? EventTaskId { get; set; }

    public Guid? EventBoatId { get; set; }

    public Guid? EventAreaId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string Status { get; set; } = "Planned";

    public string? Notes { get; set; }

    public List<CreateEventAssignmentGroupMemberRequest> Members
    {
        get;
        set;
    } = [];
}

public sealed class CreateEventAssignmentGroupMemberRequest
{
    public Guid OfficialId { get; set; }

    public string? Role { get; set; }
}

public sealed class EventAssignmentGroupResult
{
    public Guid AssignmentGroupId { get; set; }

    public IReadOnlyList<EventAssignment> Assignments { get; set; } =
        [];
}

public sealed class EventTimelineResult
{
    public Guid EventId { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public IReadOnlyList<EventAssignment> Assignments { get; set; } =
        [];

    public IReadOnlyList<OfficialTimelineRow> Officials { get; set; } =
        [];

    public IReadOnlyList<BoatTimelineRow> Boats { get; set; } =
        [];
}

public sealed class OfficialTimelineRow
{
    public Guid OfficialId { get; set; }

    public string OfficialName { get; set; } = string.Empty;

    public int PlannedMinutes { get; set; }

    public int ActualMinutes { get; set; }

    public IReadOnlyList<EventAssignment> Assignments { get; set; } =
        [];
}

public sealed class BoatTimelineRow
{
    public Guid EventBoatId { get; set; }

    public string BoatName { get; set; } = string.Empty;

    public int PlannedMinutes { get; set; }

    public IReadOnlyList<EventAssignmentGroupTimelineItem> Items
    {
        get;
        set;
    } = [];
}

public sealed class EventAssignmentGroupTimelineItem
{
    public Guid? AssignmentGroupId { get; set; }

    public Guid? EventBoatId { get; set; }

    public string? BoatName { get; set; }

    public Guid? EventTaskId { get; set; }

    public string? TaskName { get; set; }

    public Guid? EventAreaId { get; set; }

    public string? AreaName { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public IReadOnlyList<EventAssignmentGroupMember> Members
    {
        get;
        set;
    } = [];
}

public sealed class EventAssignmentGroupMember
{
    public Guid AssignmentId { get; set; }

    public Guid OfficialId { get; set; }

    public string OfficialName { get; set; } = string.Empty;

    public string? Role { get; set; }
}

public sealed class AssignmentValidationResult
{
    public bool CanAssign { get; set; }

    public IReadOnlyList<AssignmentValidationMessage> Errors
    {
        get;
        set;
    } = [];

    public IReadOnlyList<AssignmentValidationMessage> Warnings
    {
        get;
        set;
    } = [];

    public AssignmentWorkloadSummary? Workload { get; set; }
}

public sealed class AssignmentValidationMessage
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public sealed class AssignmentWorkloadSummary
{
    public Guid OfficialId { get; set; }

    public string OfficialName { get; set; } = string.Empty;

    public int ExistingPlannedMinutes { get; set; }

    public int NewAssignmentMinutes { get; set; }

    public int TotalPlannedMinutes { get; set; }

    public int? RestBeforeMinutes { get; set; }

    public int? RestAfterMinutes { get; set; }
}

public sealed class ValidateEventAssignmentRequest
{
    public Guid OfficialId { get; set; }

    public Guid? EventTaskId { get; set; }

    public Guid? EventBoatId { get; set; }

    public Guid? AssignmentGroupId { get; set; }

    public Guid? ExcludedAssignmentId { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }
}

public sealed class AssignmentCandidate
{
    public Guid OfficialId { get; set; }

    public string OfficialName { get; set; } = string.Empty;

    public bool CanAssign { get; set; }

    public int Score { get; set; }

    public int ExistingPlannedMinutes { get; set; }

    public int TotalPlannedMinutes { get; set; }

    public int? RestBeforeMinutes { get; set; }

    public int? RestAfterMinutes { get; set; }

    public IReadOnlyList<AssignmentValidationMessage> Errors
    {
        get;
        set;
    } = [];

    public IReadOnlyList<AssignmentValidationMessage> Warnings
    {
        get;
        set;
    } = [];
}

public sealed class FindAssignmentCandidatesRequest
{
    public Guid? EventTaskId { get; set; }

    public Guid? EventBoatId { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public int RequiredPeople { get; set; } = 1;
}