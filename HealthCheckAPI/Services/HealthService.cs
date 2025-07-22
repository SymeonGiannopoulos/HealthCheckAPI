using Dapper;
using HealthCheckAPI.Interface;
using HealthCheckAPI.Interfaces;
using HealthCheckAPI.Models;
using HealthCheckAPI.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HealthCheckAPI.Services
{
    public class HealthService : BackgroundService, IHealthService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHealthMemory _memory;
        private readonly Email _emailSender;
        private readonly IRetryService _retryService;

        public HealthService(IServiceScopeFactory scopeFactory, IConfiguration config, IHttpClientFactory httpClientFactory, IHealthMemory memory, Email emailSender, IRetryService retryService)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _memory = memory;
            _emailSender = emailSender;
            _retryService = retryService;

        }

        public async Task<List<object>> CheckAllInternalAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var emailSender = scope.ServiceProvider.GetRequiredService<Email>();
            var memory = scope.ServiceProvider.GetRequiredService<IHealthMemory>();

            var applications = new List<ApplicationConfigModel>();
            var connectionString = _config.GetConnectionString("SqlServerConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using var command = new SqlCommand("SELECT * FROM Applications", connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    applications.Add(new ApplicationConfigModel
                    {
                        Id = reader["Id"].ToString(),
                        Name = reader["Name"].ToString(),
                        Type = reader["Type"].ToString(),
                        HealthCheckUrl = reader["HealthCheckUrl"]?.ToString(),
                        ConnectionString = reader["ConnectionString"]?.ToString(),
                        Query = reader["Query"]?.ToString()
                    });
                }
            }

            var results = new List<object>();
            var greeceTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
            var greeceTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, greeceTimeZone);

            foreach (var app in applications)
            {
                string status = "Healthy";

                if (app.Type == "WebApp")
                {
                    try
                    {
                        var client = httpClientFactory.CreateClient();

                        var response = await _retryService.ExecuteWithRetryAsync(async () =>
                        {
                            return await client.GetAsync(app.HealthCheckUrl);
                        });

                        if (!response.IsSuccessStatusCode)
                            status = "Unhealthy";
                    }
                    catch
                    {
                        status = "Unhealthy";
                    }
                }
                else if (app.Type == "Database")
                {
                    try
                    {
                        var result = await _retryService.ExecuteWithRetryAsync(async () =>
                        {
                            using var connection = new SqlConnection(app.ConnectionString);
                            await connection.OpenAsync();

                            using var command = connection.CreateCommand();
                            command.CommandText = app.Query;
                            return await command.ExecuteScalarAsync();
                        });

                        if (result == null)
                            status = "Unhealthy";
                    }
                    catch
                    {
                        status = "Unhealthy";
                    }
                }


                using (var logConnection = new SqlConnection(connectionString))
                {
                    await logConnection.OpenAsync();
                    var sql = "INSERT INTO AppStatusLog (AppId, Status, CheckedAt) VALUES (@AppId, @Status, @CheckedAt)";
                    await logConnection.ExecuteAsync(sql, new
                    {
                        AppId = app.Id,
                        Status = status == "Healthy",
                        CheckedAt = DateTime.UtcNow
                    });
                }



                memory.StatusMap.TryGetValue(app.Id, out var previousStatus);

                if (status == "Healthy")
                {
                    if (previousStatus != "Healthy")
                    {
                        using var connection = new SqlConnection(connectionString);
                        await connection.OpenAsync();

                        var command = connection.CreateCommand();
                        command.CommandText = "DELETE FROM HealthStatusLog WHERE Id = @id";
                        command.Parameters.AddWithValue("@id", app.Id);
                        await command.ExecuteNonQueryAsync();

                        var userEmails = await GetAllUserEmailsAsync();
                        foreach (var email in userEmails)
                        {
                            emailSender.SendEmail(email,
                                $"Update: {app.Name} is Healthy",
                                $"The application {app.Name} is healthy as of {greeceTime:dd/MM/yyyy HH:mm:ss} (Greece time).");
                        }
                    }
                }
                else
                {
                    if (previousStatus != "Unhealthy")
                    {
                        await LogUnhealthyStatusAsync(app.Id, app.Name, status);

                        var userEmails = await GetAllUserEmailsAsync();
                        foreach (var email in userEmails)
                        {
                            emailSender.SendEmail(email,
                                $"Alert: {app.Name} is Unhealthy",
                                $"The application {app.Name} is unhealthy as of {greeceTime:dd/MM/yyyy HH:mm:ss} (Greece time).");
                        }
                    }
                }

                memory.StatusMap[app.Id] = status;

                results.Add(new
                {
                    Id = app.Id,
                    Name = app.Name,
                    Status = status
                });
            }

            return results;
        }

        public async Task<List<object>> CheckAllHealthStatusAsync()
        {
            var applications = new List<ApplicationConfigModel>();
            var connectionString = _config.GetConnectionString("SqlServerConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using var command = new SqlCommand("SELECT * FROM Applications", connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    applications.Add(new ApplicationConfigModel
                    {
                        Id = reader["Id"].ToString(),
                        Name = reader["Name"].ToString(),
                        Type = reader["Type"].ToString(),
                        HealthCheckUrl = reader["HealthCheckUrl"]?.ToString(),
                        ConnectionString = reader["ConnectionString"]?.ToString(),
                        Query = reader["Query"]?.ToString()
                    });
                }
            }

            var results = new List<object>();

            foreach (var app in applications)
            {
                string status = "Healthy";

                try
                {
                    if (app.Type == "WebApp")
                    {
                        var client = _httpClientFactory.CreateClient();
                        var response = await client.GetAsync(app.HealthCheckUrl);
                        if (!response.IsSuccessStatusCode)
                            status = "Unhealthy";
                    }
                    else if (app.Type == "Database")
                    {
                        using var dbConnection = new SqlConnection(app.ConnectionString);
                        await dbConnection.OpenAsync();

                        using var dbCommand = new SqlCommand(app.Query, dbConnection);
                        var result = await dbCommand.ExecuteScalarAsync();
                        if (result == null)
                            status = "Unhealthy";
                    }
                }
                catch
                {
                    status = "Unhealthy";
                }

                results.Add(new
                {
                    Id = app.Id,
                    Name = app.Name,
                    Status = status
                });
            }

            return results;
        }

        public async Task<object> CheckSingleAppHealthStatusAsync(string id)
        {
            var connectionString = _config.GetConnectionString("SqlServerConnection");

            


            ApplicationConfigModel app = null;

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using var command = new SqlCommand("SELECT * FROM Applications WHERE Id = @id", connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    app = new ApplicationConfigModel
                    {
                        Id = reader["Id"].ToString(),
                        Name = reader["Name"].ToString(),
                        Type = reader["Type"].ToString(),
                        HealthCheckUrl = reader["HealthCheckUrl"]?.ToString(),
                        ConnectionString = reader["ConnectionString"]?.ToString(),
                        Query = reader["Query"]?.ToString()
                    };
                }
            }

            if (app == null) return null;

            string status = "Healthy";

            try
            {
                if (app.Type == "WebApp")
                {
                    var client = _httpClientFactory.CreateClient();
                    var response = await client.GetAsync(app.HealthCheckUrl);
                    if (!response.IsSuccessStatusCode) status = "Unhealthy";
                }
                else if (app.Type == "Database")
                {
                    using var dbConnection = new SqlConnection(app.ConnectionString);
                    await dbConnection.OpenAsync();

                    using var dbCommand = new SqlCommand(app.Query, dbConnection);
                    var result = await dbCommand.ExecuteScalarAsync();
                    if (result == null) status = "Unhealthy";
                }
            }
            catch
            {
                status = "Unhealthy";
            }

            return new
            {
                Id = app.Id,
                Name = app.Name,
                Status = status
            };
        }


        public async Task<List<string>> GetAllUserEmailsAsync()
        {
            var emails = new List<string>();
            var connectionString = _config.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Email FROM Users WHERE Email IS NOT NULL";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                emails.Add(reader.GetString(0));
            }

            return emails;
        }

        public async Task LogUnhealthyStatusAsync(string id, string name, string status)
        {
            var connectionString = _config.GetConnectionString("SqlServerConnection");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            TimeZoneInfo greeceTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
            DateTime greeceTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, greeceTimeZone);

            using (var command1 = connection.CreateCommand())
            {
                command1.CommandText = @"
                    INSERT INTO HealthStatusLog (Id, Name, Status, Timestamp)
                    VALUES (@id, @name, @status, @timestamp)";
                command1.Parameters.AddWithValue("@id", id);
                command1.Parameters.AddWithValue("@name", name);
                command1.Parameters.AddWithValue("@status", status);
                command1.Parameters.AddWithValue("@timestamp", greeceTime);
                await command1.ExecuteNonQueryAsync();
            }

            using (var command2 = connection.CreateCommand())
            {
                command2.CommandText = @"
                    INSERT INTO ErrorLogs (AppId, Name, Status, Timestamp)
                    VALUES (@appId, @name, @status, @timestamp)";
                command2.Parameters.AddWithValue("@appId", id);
                command2.Parameters.AddWithValue("@name", name);
                command2.Parameters.AddWithValue("@status", status);
                command2.Parameters.AddWithValue("@timestamp", greeceTime);
                await command2.ExecuteNonQueryAsync();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAllInternalAsync();
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        /*public async Task<object> CheckSingleAppAsync(string id)
        {
            var connectionString = _config.GetConnectionString("SqlServerConnection");

            ApplicationConfigModel app = null;

            
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using var command = new SqlCommand("SELECT * FROM Applications WHERE Id = @id", connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    app = new ApplicationConfigModel
                    {
                        Id = reader["Id"].ToString(),
                        Name = reader["Name"].ToString(),
                        Type = reader["Type"].ToString(),
                        HealthCheckUrl = reader["HealthCheckUrl"]?.ToString(),
                        ConnectionString = reader["ConnectionString"]?.ToString(),
                        Query = reader["Query"]?.ToString()
                    };
                }
            }

            if (app == null) return null;

            string status = "Healthy";
            var greeceTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GTB Standard Time");
            var greeceTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, greeceTimeZone);

            try
            {
                if (app.Type == "WebApp")
                {
                    var client = _httpClientFactory.CreateClient();
                    var response = await client.GetAsync(app.HealthCheckUrl);
                    if (!response.IsSuccessStatusCode) status = "Unhealthy";
                }
                else if (app.Type == "Database")
                {
                    using var dbConnection = new SqlConnection(app.ConnectionString);
                    await dbConnection.OpenAsync();

                    using var dbCommand = new SqlCommand(app.Query, dbConnection);
                    var result = await dbCommand.ExecuteScalarAsync();
                    if (result == null) status = "Unhealthy";
                }
            }
            catch
            {
                status = "Unhealthy";
            }

            _memory.StatusMap.TryGetValue(app.Id, out var previousStatus);

            if (status == "Healthy")
            {
                if (previousStatus != "Healthy")
                {
                    using var conn = new SqlConnection(connectionString);
                    await conn.OpenAsync();

                    var clearCmd = new SqlCommand("DELETE FROM HealthStatusLog WHERE Id = @id", conn);
                    clearCmd.Parameters.AddWithValue("@id", app.Id);
                    await clearCmd.ExecuteNonQueryAsync();

                    var emails = await GetAllUserEmailsAsync();
                    foreach (var email in emails)
                    {
                        _emailSender.SendEmail(
                            email,
                            $"Update: {app.Name} is Healthy",
                            $"The application {app.Name} is healthy as of {greeceTime:dd/MM/yyyy HH:mm:ss} (Greece time)."
                        );
                    }
                }
            }
            else
            {
                if (previousStatus != "Unhealthy")
                {
                    await LogUnhealthyStatusAsync(app.Id, app.Name, status);

                    var emails = await GetAllUserEmailsAsync();
                    foreach (var email in emails)
                    {
                        _emailSender.SendEmail(
                            email,
                            $"Alert: {app.Name} is Unhealthy",
                            $"The application {app.Name} is unhealthy as of {greeceTime:dd/MM/yyyy HH:mm:ss} (Greece time)."
                        );
                    }
                }
            }

            _memory.StatusMap[app.Id] = status;

            return new
            {
                Id = app.Id,
                Name = app.Name,
                Status = status
            };
        }*/

       




    }
}
