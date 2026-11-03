using Dapper;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public class BoatDefectService
{
    private readonly IConfiguration _configuration;
    public BoatDefectService(IConfiguration configuration) => _configuration = configuration;
    private NpgsqlConnection Connect() => new(_configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing database connection."));

    public async Task<IReadOnlyList<BoatDefect>> GetByBoatAsync(Guid boatId)
    {
        await using var connection = Connect();
        var items = await connection.QueryAsync<BoatDefect>(
            "SELECT * FROM \"BoatDefects\" WHERE \"BoatId\" = @boatId ORDER BY \"ReportedAt\" DESC", new { boatId });
        return items.ToList();
    }

    public async Task<BoatDefect?> CreateAsync(Guid boatId, BoatDefect defect)
    {
        await using var connection = Connect();
        if (!await connection.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM \"Boats\" WHERE \"Id\" = @boatId)", new { boatId }))
            return null;
        defect.Id = Guid.NewGuid();
        defect.BoatId = boatId;
        defect.ReportedAt = DateTime.UtcNow;
        defect.CreatedAt = DateTime.UtcNow;
        defect.Status = "Reported";
        await connection.ExecuteAsync("""
            INSERT INTO "BoatDefects" ("Id", "BoatId", "ReportedAt", "ReportedBy", "Description", "Priority", "Status", "CreatedAt")
            VALUES (@Id, @BoatId, @ReportedAt, @ReportedBy, @Description, @Priority, @Status, @CreatedAt)
            """, defect);
        return defect;
    }

    public async Task<bool> UpdateStatusAsync(Guid boatId, Guid defectId, string status)
    {
        await using var connection = Connect();
        var rows = await connection.ExecuteAsync("""
            UPDATE "BoatDefects" SET "Status" = @status
            WHERE "Id" = @defectId AND "BoatId" = @boatId
            """, new { boatId, defectId, status });
        return rows > 0;
    }
}
