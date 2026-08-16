using EventHelm.Api.Models;

namespace EventHelm.Api.Interfaces;

public interface IEventAssignmentImportService
{
    Task<EventAssignmentImportPreview> PreviewAsync(
        Guid eventId,
        Stream fileStream,
        string fileName);

    Task<EventAssignmentImportResult> CommitAsync(
        Guid eventId,
        EventAssignmentImportCommitRequest request);
}
