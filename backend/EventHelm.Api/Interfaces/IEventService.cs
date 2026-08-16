using EventHelm.Api.Models;

namespace EventHelm.Api.Interfaces;

public interface IEventService
{
    Task<IReadOnlyList<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(Guid id);
    Task<Event> CreateAsync(Event eventItem);
    Task<Event?> UpdateAsync(Guid id, Event eventItem);
    Task<bool> DeleteAsync(Guid id);
}
