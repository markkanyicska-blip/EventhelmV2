using Dapper;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public class UserService : IUserService
{
    private readonly IConfiguration _configuration;

    public UserService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private NpgsqlConnection CreateConnection()
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A DefaultConnection nincs beállítva.");

        return new NpgsqlConnection(connectionString);
    }

    public async Task<List<User>> GetAllAsync()
    {
        await using var connection = CreateConnection();

        var users = await connection.QueryAsync<User>(
            """
            SELECT
                "Id",
                "Name",
                "Email",
                "Phone",
                "Role",
                "IsActive",
                "CreatedAt"
            FROM "Users"
            ORDER BY "Name";
            """);

        return users.ToList();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<User>(
            """
            SELECT
                "Id",
                "Name",
                "Email",
                "Phone",
                "Role",
                "IsActive",
                "CreatedAt"
            FROM "Users"
            WHERE "Id" = @Id;
            """,
            new { Id = id });
    }

    public async Task<User> CreateAsync(User user)
    {
        await using var connection = CreateConnection();

        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO "Users"
            (
                "Id",
                "Name",
                "Email",
                "Phone",
                "Role",
                "IsActive",
                "CreatedAt"
            )
            VALUES
            (
                @Id,
                @Name,
                @Email,
                @Phone,
                @Role,
                @IsActive,
                @CreatedAt
            );
            """,
            user);

        return user;
    }

    public async Task<User?> UpdateAsync(Guid id, User user)
    {
        await using var connection = CreateConnection();

        var rows = await connection.ExecuteAsync(
            """
            UPDATE "Users"
            SET
                "Name" = @Name,
                "Email" = @Email,
                "Phone" = @Phone,
                "Role" = @Role,
                "IsActive" = @IsActive
            WHERE "Id" = @Id;
            """,
            new
            {
                Id = id,
                user.Name,
                user.Email,
                user.Phone,
                user.Role,
                user.IsActive
            });

        if (rows == 0)
        {
            return null;
        }

        user.Id = id;
        return user;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = CreateConnection();

        var rows = await connection.ExecuteAsync(
            """
            DELETE FROM "Users"
            WHERE "Id" = @Id;
            """,
            new { Id = id });

        return rows > 0;
    }
}