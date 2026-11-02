using EventHelm.Api.Interfaces;
using EventHelm.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using EventHelm.Api.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
         .WithOrigins("http://localhost:5173")
.AllowAnyHeader()
.AllowAnyMethod()
.AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "EventHelm.Access";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
  options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});
builder.Services.AddOpenApi();

// Alap szolgáltatások
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IOfficialService, OfficialService>();
builder.Services.AddScoped<IUserService, UserService>();

// Hajók
builder.Services.AddScoped<IBoatService, BoatService>();
builder.Services.AddScoped<BoatDefectService>();
builder.Services.AddScoped<BoatDefectService>();
builder.Services.AddScoped<BoatDefectService>();
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

app.UseAuthentication();
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