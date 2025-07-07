using HealthCheckAPI;
using HealthCheckAPI.Controllers;
using HealthCheckAPI.Interfaces;
using HealthCheckAPI.Notifications;
using HealthCheckAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.Sqlite;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddHttpClient();


builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


using (var connection = new SqliteConnection(builder.Configuration.GetConnectionString("SqliteConnection")))
{
    connection.Open();

    var command = connection.CreateCommand();
    command.CommandText = @"
        CREATE TABLE IF NOT EXISTS HealthStatusLog (
            Id TEXT,
            Name TEXT,
            Status TEXT,
            Timestamp TEXT
        );
    ";

    command.ExecuteNonQuery();
}
builder.Services.AddHttpClient();
builder.Services.AddTransient<Email>();
builder.Services.AddHostedService<HealthCheckService>();
builder.Services.AddTransient<HealthController>();
builder.Services.AddSingleton<IHealthMemory, HealthMemory>();







var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
