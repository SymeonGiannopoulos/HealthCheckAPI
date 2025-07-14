using HealthCheckAPI;
using HealthCheckAPI.Controllers;
using HealthCheckAPI.Interface;
using HealthCheckAPI.Models;
using HealthCheckAPI.Notifications;
using HealthCheckAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ========================
// Services Registration
// ========================
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<IPasswordHasher<UserModel>, PasswordHasher<UserModel>>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddSingleton<IHealthMemory, HealthMemory>();
builder.Services.AddTransient<Email>();
builder.Services.AddHostedService<HealthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JwtService>();

// Optional (not usually required)
builder.Services.AddTransient<HealthController>();

// ========================
// JWT Configuration
// ========================
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettingsModel>(jwtSettingsSection);

var jwtSettings = jwtSettingsSection.Get<JwtSettingsModel>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };
});

// ========================
// Swagger Configuration
// ========================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HealthCheck API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your valid JWT token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// ========================
// SQL Table Creation
// ========================
using (var connection = new SqlConnection(builder.Configuration.GetConnectionString("SqlServerConnection")))
{
    connection.Open();
    var command = connection.CreateCommand();

    command.CommandText = @"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HealthStatusLog')
        BEGIN
            CREATE TABLE HealthStatusLog (
                Id NVARCHAR(50),
                Name NVARCHAR(100),
                Status NVARCHAR(50),
                Timestamp DATETIME
            );
        END";
    command.ExecuteNonQuery();

    command.CommandText = @"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
        BEGIN
            CREATE TABLE Users (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Username NVARCHAR(100) NOT NULL UNIQUE,
                Password NVARCHAR(256) NOT NULL,
                Email NVARCHAR(256)
            );
        END";
    command.ExecuteNonQuery();

    command.CommandText = @"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ErrorLogs')
        BEGIN
            CREATE TABLE ErrorLogs (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                AppId NVARCHAR(50) NOT NULL,
                Name NVARCHAR(100) NOT NULL,
                Status NVARCHAR(50),
                Timestamp DATETIME
            );
        END";
    command.ExecuteNonQuery();

    command.CommandText = @"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Applications')
        BEGIN
            CREATE TABLE Applications (
                Id NVARCHAR(50) PRIMARY KEY,
                Name NVARCHAR(100) NOT NULL,
                Type NVARCHAR(50),
                HealthCheckUrl NVARCHAR(200),
                ConnectionString NVARCHAR(MAX),
                Query NVARCHAR(MAX)
            );
        END";
    command.ExecuteNonQuery();
}

// ========================
// App Pipeline
// ========================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Uncomment if you use HTTPS

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
