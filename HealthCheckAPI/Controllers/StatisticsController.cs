using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using HealthCheckAPI.Models;

namespace HealthCheckAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StatisticsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetStatistics()
        {
            var connectionString = _configuration.GetConnectionString("SqlServerConnection");
            var statsByApp = new Dictionary<string, AppStatisticsModel>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var command = new SqlCommand("SELECT Name, Status, Timestamp FROM ErrorLogs ORDER BY Name, Timestamp", connection);

                using (var reader = command.ExecuteReader())
                {
                    string currentApp = null;
                    string previousStatus = null;
                    DateTime? downtimeStart = null;
                    DateTime? firstTimestamp = null;
                    DateTime? lastTimestamp = null;

                    while (reader.Read())
                    {
                        var name = reader["Name"].ToString();
                        var status = reader["Status"].ToString();
                        var timestamp = (DateTime)reader["Timestamp"];

                        if (!statsByApp.ContainsKey(name))
                        {
                            statsByApp[name] = new AppStatisticsModel
                            {
                                Name = name,
                                DowntimeCount = 0,
                                TotalDowntimeDuration = TimeSpan.Zero,
                                MeanDowntimeDuration = TimeSpan.Zero,
                                AvailabilityPercentage = 0
                            };

                            firstTimestamp = timestamp; // πρώτο log για αυτήν την εφαρμογή
                        }

                        lastTimestamp = timestamp; // ανανεώνεται συνεχώς μέχρι το τέλος

                        if (currentApp != name)
                        {
                            currentApp = name;
                            previousStatus = null;
                            downtimeStart = null;
                            firstTimestamp = timestamp;
                        }

                        var appStats = statsByApp[name];

                        if (previousStatus != null)
                        {
                            if (previousStatus == "Healthy" && status == "Unhealthy")
                            {
                                downtimeStart = timestamp;
                                appStats.DowntimeCount++;
                            }
                            else if (previousStatus == "Unhealthy" && status == "Healthy" && downtimeStart.HasValue)
                            {
                                var downtimeDuration = timestamp - downtimeStart.Value;
                                appStats.TotalDowntimeDuration += downtimeDuration;
                                downtimeStart = null;
                            }
                        }

                        previousStatus = status;
                    }

                    // Τελικός υπολογισμός για κάθε εφαρμογή
                    foreach (var appStat in statsByApp.Values)
                    {
                        if (appStat.DowntimeCount > 0)
                        {
                            appStat.MeanDowntimeDuration = TimeSpan.FromTicks(
                                appStat.TotalDowntimeDuration.Ticks / appStat.DowntimeCount
                            );
                        }

                        if (firstTimestamp.HasValue && lastTimestamp.HasValue && lastTimestamp > firstTimestamp)
                        {
                            var observedPeriod = lastTimestamp.Value - firstTimestamp.Value;
                            var uptime = observedPeriod - appStat.TotalDowntimeDuration;

                            appStat.AvailabilityPercentage = Math.Max(0,
                                (uptime.TotalSeconds / observedPeriod.TotalSeconds) * 100);
                        }
                        else
                        {
                            appStat.AvailabilityPercentage = 100;
                        }
                    }
                }
            }

            return Ok(statsByApp.Values);
        }
    }
}
