using AspNetCoreRateLimit;
using HealthCheckAPI.Controllers;
using HealthCheckAPI.Interface;
using HealthCheckAPI.Interface;
using HealthCheckAPI.Interfaces;
using HealthCheckAPI.Models;
using HealthCheckAPI.Notifications;
using HealthCheckAPI.Repositories;
using HealthCheckAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using System;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<IPasswordHasher<UserModel>, PasswordHasher<UserModel>>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddSingleton<IHealthMemory, HealthMemoryService>();
builder.Services.AddTransient<Email>();
builder.Services.AddHostedService<HealthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IAppStatusLogRepository, AppStatusLogRepository>();
builder.Services.AddScoped<IAppStatisticsService, AppStatisticsService>();
builder.Services.AddSingleton<IRetryService, RetryService>();
builder.Services.AddHealthChecks();

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();




builder.Services.AddTransient<HealthController>();


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
        Description = "Enter your valid JWT token."
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

    command.CommandText = @"
        IF NOT EXISTS(SELECT * FROM sys.tables WHERE name = 'AppStatusLog')
        BEGIN
        CREATE TABLE AppStatusLog (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            AppId NVARCHAR(100) NOT NULL,
            Status BIT NOT NULL, 
            CheckedAt DATETIME NOT NULL
);
        END";
    command.ExecuteNonQuery();

}


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
app.UseIpRateLimiting(); 

app.UseHttpMetrics(); 

app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics(); 
});


app.UseHttpsRedirection(); 

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
