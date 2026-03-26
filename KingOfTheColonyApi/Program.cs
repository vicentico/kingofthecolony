using System.Text;
using KingOfTheColonyApi.Data;
using KingOfTheColonyApi.Extensions;
using KingOfTheColonyApi.Hubs;
using KingOfTheColonyApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["PORT"];
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// --- Database ---
var defaultConnection = builder.Configuration.GetRequiredConnectionString("DefaultConnection", "ConnectionStrings__DefaultConnection");
var migrationConnection = builder.Configuration.GetOptionalConnectionString("MigrationConnection", "ConnectionStrings__MigrationConnection");
var applyMigrationsOnStartup = builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultConnection, npgsqlOptions => npgsqlOptions.MaxBatchSize(1)));

// --- Authentication (JWT) ---
var jwtKey = builder.Configuration.GetRequiredValue("Jwt:Key", "Jwt__Key");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Allow SignalR to use JWT from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/game"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// --- Services ---
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<PasswordHashService>();
builder.Services.AddScoped<GameRoomService>();
builder.Services.AddScoped<RankingService>();
builder.Services.AddScoped<CreditService>();

// --- Controllers + SignalR ---
builder.Services.AddControllers();
builder.Services.AddSignalR();

// --- CORS (allow WPF desktop client) ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });

    options.AddPolicy("SignalR", policy =>
    {
        policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});

var app = builder.Build();

// --- Auto-migrate database ---
if (applyMigrationsOnStartup && !string.IsNullOrWhiteSpace(migrationConnection))
{
    var migrationOptions = new DbContextOptionsBuilder<AppDbContext>();
    migrationOptions.UseNpgsql(migrationConnection, npgsqlOptions => npgsqlOptions.MaxBatchSize(1));

    using var migrationDb = new AppDbContext(migrationOptions.Options);
    migrationDb.Database.Migrate();
}
else
{
    app.Logger.LogInformation("Skipping startup migrations. Enable Database:ApplyMigrationsOnStartup and configure ConnectionStrings:MigrationConnection to run them.");
}

app.UseCors("SignalR");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.Run();
