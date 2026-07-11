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

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IOfficialService, OfficialService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBoatService, BoatService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        application = "EventHelm.Api",
        timestamp = DateTimeOffset.UtcNow
    });
});

app.Run();