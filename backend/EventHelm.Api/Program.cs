using EventHelm.Api.Interfaces;
using EventHelm.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

// Alap szolgáltatások
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IOfficialService, OfficialService>();
builder.Services.AddScoped<IUserService, UserService>();

// Hajók
builder.Services.AddScoped<IBoatService, BoatService>();
builder.Services.AddScoped<IBoatCrewService, BoatCrewService>();

// Rendezvényhez kapcsolódó szolgáltatások
builder.Services.AddScoped<EventAreaService>();
builder.Services.AddScoped<EventBoatService>();
builder.Services.AddScoped<EventAssignmentService>();
builder.Services.AddScoped<EventTimelineService>();
builder.Services.AddScoped<EventTaskService>();

// Rendezői beosztások
builder.Services.AddScoped<
    IEventOfficialAssignmentService,
    EventOfficialAssignmentService
>();

builder.Services.AddScoped<
    IEventAssignmentImportService,
    EventAssignmentImportService
>();

// Race Control
builder.Services.AddSingleton<
    IRaceControlService,
    JsonRaceControlService
>();

builder.Services.AddSingleton<
    IRadioAssignmentService,
    JsonRadioAssignmentService
>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/health", () =>
    Results.Ok(new
    {
        status = "ok",
        service = "EventHelm.Api",
        version = "EventHelm V2",
        timestamp = DateTime.UtcNow
    }));

app.Run();