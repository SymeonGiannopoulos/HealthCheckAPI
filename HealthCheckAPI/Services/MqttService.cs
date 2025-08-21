using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Threading.Tasks;

namespace HealthCheckAPI.Services
{
    public class MqttService
    {
        private readonly IMqttClient _client;
        private readonly MqttClientOptions _options;

        public MqttService()
        {
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .Build();

            _client.ConnectedAsync += async e =>
            {
                Console.WriteLine("✅ Συνδέθηκε με MQTT Broker.");
                await Task.CompletedTask;
            };

            _client.DisconnectedAsync += async e =>
            {
                Console.WriteLine("⚠️ Αποσυνδέθηκε. Προσπάθεια επανασύνδεσης...");
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    await _client.ConnectAsync(_options);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Απέτυχε η επανασύνδεση: " + ex.Message);
                }
            };

            _client.ConnectAsync(_options).GetAwaiter().GetResult();
        }

        public async Task PublishStatusAsync(string status)
        {
            if (!_client.IsConnected)
            {
                Console.WriteLine("⚠️ Δεν υπάρχει σύνδεση με broker. Παράλειψη publish.");
                return;
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("notifications")
                .WithPayload(status)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(message);
        }
    }
}
