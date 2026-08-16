using EventHelm.Api.Models;

namespace EventHelm.Api.Interfaces;

public interface IEventOfficialAssignmentService
{
    Task<IReadOnlyList<EventOfficialAssignmentDetails>> GetByEventAsync(
        Guid eventId);

    Task<EventOfficialAssignmentDetails?> GetByIdAsync(
        Guid eventId,
        Guid assignmentId);

    Task<EventOfficialAssignmentDetails> CreateAsync(
        Guid eventId,
        SaveEventOfficialAssignmentRequest request);

    Task<EventOfficialAssignmentDetails?> UpdateAsync(
        Guid eventId,
        Guid assignmentId,
        SaveEventOfficialAssignmentRequest request);

    Task<bool> DeleteAsync(
        Guid eventId,
        Guid assignmentId);
}
