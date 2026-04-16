#pragma warning disable CS1591
namespace MESS.Services.TrainingMatrix;

public sealed class TrainingMatrixMqttOptions
{
    public const string SectionName = "TrainingMatrixMqtt";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = "mess-training-matrix";
    public string Topic { get; set; } = "mess/training-matrix/updates";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public ushort KeepAliveSeconds { get; set; } = 30;
    public int ReconnectDelaySeconds { get; set; } = 5;
}
#pragma warning restore CS1591
