using EventHelm.Api.Models;

namespace EventHelm.Api.Services;

public interface IRadioAssignmentService
{
    Task<IReadOnlyList<RadioEquipment>> GetRadiosAsync(CancellationToken ct);
    Task<RadioEquipment> CreateRadioAsync(SaveRadioRequest request, CancellationToken ct);
    Task<RadioEquipment?> UpdateRadioAsync(string id, SaveRadioRequest request, CancellationToken ct);
    Task<IReadOnlyList<RadioAssignmentView>> GetAssignmentsAsync(string eventId, CancellationToken ct);
    Task<EventOfficialRadioAssignment> CreateAssignmentAsync(string eventId, SaveRadioAssignmentRequest request, CancellationToken ct);
    Task<EventOfficialRadioAssignment?> UpdateAssignmentAsync(string eventId, string assignmentId, SaveRadioAssignmentRequest request, CancellationToken ct);
    Task<bool> DeleteAssignmentAsync(string eventId, string assignmentId, CancellationToken ct);
}
