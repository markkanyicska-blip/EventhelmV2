using EventHelm.Api.Models;

namespace EventHelm.Api.Services;

public interface IBoatService
{
    Task<IReadOnlyList<Boat>> GetAllAsync();

    Task<Boat?> GetByIdAsync(Guid id);

    Task<Boat> CreateAsync(Boat boat);

    Task<Boat?> UpdateAsync(Guid id, Boat boat);

    Task<bool> DeleteAsync(Guid id);
}