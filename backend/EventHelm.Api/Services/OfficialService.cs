using Dapper;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public class OfficialService : IOfficialService
{
    private readonly IConfiguration _configuration;

    public OfficialService(IConfiguration configuration)
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

    public async Task<List<Official>> GetAllAsync()
    {
        await using var connection = CreateConnection();

        var officials = await connection.QueryAsync<Official>(
            """
            SELECT
                "Id",
                "Name",
                "Phone",
                "Email",
                "Role",
                "Status",
                "CreatedAt"
            FROM "Officials"
            ORDER BY "Name";
            """);

        return officials.ToList();
    }

    public async Task<Official?> GetByIdAsync(Guid id)
    {
        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<Official>(
            """
            SELECT
                "Id",
                "Name",
                "Phone",
                "Email",
                "Role",
                "Status",
                "CreatedAt"
            FROM "Officials"
            WHERE "Id" = @Id;
            """,
            new { Id = id });
    }

    public async Task<Official> CreateAsync(Official official)
    {
        await using var connection = CreateConnection();

        official.Id = Guid.NewGuid();
        official.CreatedAt = DateTime.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO "Officials"
            (
                "Id",
                "Name",
                "Phone",
                "Email",
                "Role",
                "Status",
                "CreatedAt"
            )
            VALUES
            (
                @Id,
                @Name,
                @Phone,
                @Email,
                @Role,
                @Status,
                @CreatedAt
            );
            """,
            official);

        return official;
    }

    public async Task<Official?> UpdateAsync(Guid id, Official official)
    {
        await using var connection = CreateConnection();

        var rows = await connection.ExecuteAsync(
            """
            UPDATE "Officials"
            SET
                "Name" = @Name,
                "Phone" = @Phone,
                "Email" = @Email,
                "Role" = @Role,
                "Status" = @Status
            WHERE "Id" = @Id;
            """,
            new
            {
                Id = id,
                official.Name,
                official.Phone,
                official.Email,
                official.Role,
                official.Status
            });

        if (rows == 0)
        {
            return null;
        }

        official.Id = id;
        return official;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = CreateConnection();

        var rows = await connection.ExecuteAsync(
            """
            DELETE FROM "Officials"
            WHERE "Id" = @Id;
            """,
            new { Id = id });

        return rows > 0;
    }
}