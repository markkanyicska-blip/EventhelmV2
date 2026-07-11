using EventHelm.Api.Models;

namespace EventHelm.Api.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<User>> GetAllAsync();

    Task<User?> GetByIdAsync(Guid id);

    Task<User> CreateAsync(User user);

    Task<User?> UpdateAsync(Guid id, User user);

    Task<bool> DeleteAsync(Guid id);
}