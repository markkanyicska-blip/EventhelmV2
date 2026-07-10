using EventHelm.Api.Models;

namespace EventHelm.Api.Services;

public interface IOfficialService
{
    Task<List<Official>> GetAllAsync();
    Task<Official?> GetByIdAsync(Guid id);
    Task<Official> CreateAsync(Official official);
    Task<Official?> UpdateAsync(Guid id, Official official);
    Task<bool> DeleteAsync(Guid id);
}