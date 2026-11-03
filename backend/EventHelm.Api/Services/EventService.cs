using Dapper;
using EventHelm.Api.Interfaces;
using EventHelm.Api.Models;
using Npgsql;

namespace EventHelm.Api.Services;

public class EventService : IEventService
{
    private readonly IConfiguration _configuration;

    public EventService(IConfiguration configuration)
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
    public async Task<IReadOnlyList<Event>> GetAllAsync()
    {
        await using var connection = CreateConnection();

        var items = await connection.QueryAsync<Event>(
            """
            SELECT
                "Id",
                "Name",
                "Status",
                "OrganizingClub",
                "HostingClub",
                "StartDate",
                "EndDate",
                "Location",
                "WaterArea",
                "RaceOfficer",
                "JuryChair",
                "TechnicalCommitteeChair",
                "RaceOffice",
                "VhfChannel",
                "TrunkGroup",
                "Notes"
            FROM "Events"
            ORDER BY
                "StartDate" NULLS LAST,
                "Name";
            """);

        return items.ToList();
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        await using var connection = CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<Event>(
            """
            SELECT
                "Id",
                "Name",
                "Status",
                "OrganizingClub",
                "HostingClub",
                "StartDate",
                "EndDate",
                "Location",
                "WaterArea",
                "RaceOfficer",
                "JuryChair",
                "TechnicalCommitteeChair",
                "RaceOffice",
                "VhfChannel",
                "TrunkGroup",
                "Notes"
            FROM "Events"
            WHERE "Id" = @Id;
            """,
            new
            {
                Id = id
            });
    }

    public async Task<Event> CreateAsync(Event eventItem)
    {
        await using var connection = CreateConnection();

        eventItem.Id = Guid.NewGuid();

        await connection.ExecuteAsync(
            """
            INSERT INTO "Events"
            (
                "Id",
                "Name",
                "Status",
                "OrganizingClub",
                "HostingClub",
                "StartDate",
                "EndDate",
                "Location",
                "WaterArea",
                "RaceOfficer",
                "JuryChair",
                "TechnicalCommitteeChair",
                "RaceOffice",
                "VhfChannel",
                "TrunkGroup",
                "Notes"
            )
            VALUES
            (
                @Id,
                @Name,
                @Status,
                @OrganizingClub,
                @HostingClub,
                @StartDate,
                @EndDate,
                @Location,
                @WaterArea,
                @RaceOfficer,
                @JuryChair,
                @TechnicalCommitteeChair,
                @RaceOffice,
                @VhfChannel,
                @TrunkGroup,
                @Notes
            );
            """,
            new
            {
                eventItem.Id,
                eventItem.Name,
                eventItem.Status,
                eventItem.OrganizingClub,
                eventItem.HostingClub,
                eventItem.StartDate,
                eventItem.EndDate,
                eventItem.Location,
                eventItem.WaterArea,
                eventItem.RaceOfficer,
                eventItem.JuryChair,
                eventItem.TechnicalCommitteeChair,
                eventItem.RaceOffice,
                eventItem.VhfChannel,
                eventItem.TrunkGroup,
                eventItem.Notes
            });

        return eventItem;
    }

    public async Task<Event?> UpdateAsync(
        Guid id,
        Event eventItem)
    {
        await using var connection = CreateConnection();

        var rowsAffected =
            await connection.ExecuteAsync(
                """
                UPDATE "Events"
                SET
                    "Name" = @Name,
                    "Status" = @Status,
                    "OrganizingClub" = @OrganizingClub,
                    "HostingClub" = @HostingClub,
                    "StartDate" = @StartDate,
                    "EndDate" = @EndDate,
                    "Location" = @Location,
                    "WaterArea" = @WaterArea,
                    "RaceOfficer" = @RaceOfficer,
                    "JuryChair" = @JuryChair,
                    "TechnicalCommitteeChair" = @TechnicalCommitteeChair,
                    "RaceOffice" = @RaceOffice,
                    "VhfChannel" = @VhfChannel,
                    "TrunkGroup" = @TrunkGroup,
                    "Notes" = @Notes
                WHERE "Id" = @Id;
                """,
                new
                {
                    Id = id,
                    Name = eventItem.Name,
                    Status = eventItem.Status,
                    OrganizingClub = eventItem.OrganizingClub,
                    HostingClub = eventItem.HostingClub,
                    StartDate = eventItem.StartDate,
                    EndDate = eventItem.EndDate,
                    Location = eventItem.Location,
                    WaterArea = eventItem.WaterArea,
                    RaceOfficer = eventItem.RaceOfficer,
                    JuryChair = eventItem.JuryChair,
                    TechnicalCommitteeChair =
                        eventItem.TechnicalCommitteeChair,
                    RaceOffice = eventItem.RaceOffice,
                    VhfChannel = eventItem.VhfChannel,
                    TrunkGroup = eventItem.TrunkGroup,
                    Notes = eventItem.Notes
                });

        if (rowsAffected == 0)
        {
            return null;
        }

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = CreateConnection();

        var rowsAffected =
            await connection.ExecuteAsync(
                """
                DELETE FROM "Events"
                WHERE "Id" = @Id;
                """,
                new
                {
                    Id = id
                });

        return rowsAffected > 0;
    }
}
