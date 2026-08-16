using EventHelm.Api.Models;

namespace EventHelm.Api.Services;

public interface IBoatCrewService
{
    Task<IReadOnlyList<BoatCrewAssignment>> GetByBoatIdAsync(
        Guid boatId);

    Task<BoatCrewAssignment?> GetByIdAsync(
        Guid id);

    Task<BoatCrewAssignment> CreateAsync(
        Guid boatId,
        CreateBoatCrewAssignmentRequest request);

    Task<BoatCrewAssignment?> UpdateAsync(
        Guid id,
        UpdateBoatCrewAssignmentRequest request);

    Task<bool> DeleteAsync(Guid id);
}