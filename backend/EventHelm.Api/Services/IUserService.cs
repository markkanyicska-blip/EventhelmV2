using EventHelm.Api.Models;

namespace EventHelm.Api.Services;

public interface IUserService
{
    Task<List<User>> GetAllAsync();

    Task<User?> GetByIdAsync(Guid id);

    Task<User> CreateAsync(User user);

    Task<User?> UpdateAsync(Guid id, User user);

    Task<bool> DeleteAsync(Guid id);
}