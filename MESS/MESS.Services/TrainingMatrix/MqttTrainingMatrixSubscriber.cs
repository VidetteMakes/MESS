#pragma warning disable CS1591
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace MESS.Services.TrainingMatrix;

public sealed class MqttTrainingMatrixSubscriber : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ITrainingMatrixStatusStore _statusStore;
    private readonly TrainingMatrixMqttOptions _options;

    public MqttTrainingMatrixSubscriber(
        ITrainingMatrixStatusStore statusStore,
        IOptions<TrainingMatrixMqttOptions> options)
    {
        _statusStore = statusStore;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _statusStore.SetMqttState(CreateState(isConnected: false, lastError: "MQTT integration is disabled."));
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConnectionLoopAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Training matrix MQTT subscriber disconnected");
                _statusStore.SetMqttState(CreateState(isConnected: false, lastError: ex.Message));
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.ReconnectDelaySeconds)), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunConnectionLoopAsync(CancellationToken cancellationToken)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(_options.Host, _options.Port, cancellationToken);

        await using var stream = tcpClient.GetStream();
        await SendConnectAsync(stream, cancellationToken);
        await ReadConnAckAsync(stream, cancellationToken);
        await SendSubscribeAsync(stream, cancellationToken);

        _statusStore.SetMqttState(CreateState(isConnected: true));
        Log.Information("Connected training matrix MQTT subscriber to {Host}:{Port} topic {Topic}", _options.Host, _options.Port, _options.Topic);

        var pingTask = RunPingLoopAsync(stream, cancellationToken);
        var receiveTask = ReceivePacketsAsync(stream, cancellationToken);
        await Task.WhenAny(pingTask, receiveTask);
        await receiveTask;
    }

    private async Task ReceivePacketsAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var packetHeader = await ReadByteAsync(stream, cancellationToken);
            var remainingLength = await ReadRemainingLengthAsync(stream, cancellationToken);
            var body = await ReadExactAsync(stream, remainingLength, cancellationToken);

            switch (packetHeader >> 4)
            {
                case 3:
                    await HandlePublishAsync(packetHeader, body);
                    break;
                case 9:
                case 13:
                    break;
                default:
                    Log.Debug("Ignoring MQTT control packet type {PacketType}", packetHeader >> 4);
                    break;
            }
        }
    }

    private async Task HandlePublishAsync(byte packetHeader, byte[] body)
    {
        var qos = (packetHeader & 0b0000_0110) >> 1;
        var position = 0;
        var topicLength = ReadUInt16(body, ref position);
        var topic = Encoding.UTF8.GetString(body, position, topicLength);
        position += topicLength;

        if (qos > 0)
        {
            position += 2;
        }

        var payload = Encoding.UTF8.GetString(body, position, body.Length - position);
        if (!string.Equals(topic, _options.Topic, StringComparison.Ordinal))
        {
            return;
        }

        var message = JsonSerializer.Deserialize<MqttTrainingMatrixUpdateMessage>(payload, SerializerOptions);
        if (message is null || message.Score is null)
        {
            Log.Warning("Received invalid training matrix MQTT payload");
            return;
        }

        var score = Math.Clamp(message.Score.Value, 0, 5);
        var record = new TrainingMatrixScoreRecord
        {
            UserId = NormalizeToken(message.UserId),
            UserName = NormalizeToken(message.UserName),
            ModuleId = message.ModuleId,
            ModuleTitle = NormalizeToken(message.ModuleTitle),
            StepId = message.StepId,
            StepTitle = NormalizeToken(message.StepTitle),
            Score = score,
            LastUpdatedOnUtc = DateTimeOffset.UtcNow,
            Source = string.IsNullOrWhiteSpace(message.Source) ? "mqtt" : message.Source.Trim()
        };

        await _statusStore.UpsertAsync(record);
        _statusStore.SetMqttState(CreateState(isConnected: true, lastMessageOnUtc: record.LastUpdatedOnUtc));
        Log.Information("Processed training matrix MQTT update for user {UserName} step {StepTitle} score {Score}",
            record.UserName ?? record.UserId,
            record.StepTitle ?? record.StepId?.ToString(),
            record.Score);
    }

    private async Task RunPingLoopAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _options.KeepAliveSeconds / 2)), cancellationToken);
            await stream.WriteAsync(new byte[] { 0xC0, 0x00 }, cancellationToken);
        }
    }

    private async Task SendConnectAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var variableHeader = new List<byte>();
        variableHeader.AddRange(EncodeString("MQTT"));
        variableHeader.Add(0x04);

        var connectFlags = (byte)0b0000_0010;
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            connectFlags |= 0b1000_0000;
        }

        if (!string.IsNullOrWhiteSpace(_options.Password))
        {
            connectFlags |= 0b0100_0000;
        }

        variableHeader.Add(connectFlags);
        variableHeader.AddRange(EncodeUInt16(_options.KeepAliveSeconds));

        var payload = new List<byte>();
        payload.AddRange(EncodeString(_options.ClientId));

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            payload.AddRange(EncodeString(_options.Username));
        }

        if (!string.IsNullOrWhiteSpace(_options.Password))
        {
            payload.AddRange(EncodeString(_options.Password));
        }

        var packet = new List<byte> { 0x10 };
        packet.AddRange(EncodeRemainingLength(variableHeader.Count + payload.Count));
        packet.AddRange(variableHeader);
        packet.AddRange(payload);
        await stream.WriteAsync(packet.ToArray(), cancellationToken);
    }

    private async Task ReadConnAckAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var packetType = await ReadByteAsync(stream, cancellationToken);
        if ((packetType >> 4) != 2)
        {
            throw new InvalidOperationException($"Expected CONNACK packet but received MQTT packet type {packetType >> 4}.");
        }

        var remainingLength = await ReadRemainingLengthAsync(stream, cancellationToken);
        var body = await ReadExactAsync(stream, remainingLength, cancellationToken);
        if (body.Length < 2 || body[1] != 0x00)
        {
            throw new InvalidOperationException($"MQTT connection rejected with return code {(body.Length > 1 ? body[1] : (byte)255)}.");
        }
    }

    private async Task SendSubscribeAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var packet = new List<byte> { 0x82 };
        var variableHeader = EncodeUInt16(1);
        var payload = new List<byte>();
        payload.AddRange(EncodeString(_options.Topic));
        payload.Add(0x00);

        packet.AddRange(EncodeRemainingLength(variableHeader.Length + payload.Count));
        packet.AddRange(variableHeader);
        packet.AddRange(payload);
        await stream.WriteAsync(packet.ToArray(), cancellationToken);
    }

    private TrainingMatrixMqttState CreateState(
        bool isConnected,
        DateTimeOffset? lastMessageOnUtc = null,
        string? lastError = null)
    {
        var currentState = _statusStore.GetMqttState();
        return new TrainingMatrixMqttState
        {
            IsEnabled = _options.Enabled,
            IsConnected = isConnected,
            Broker = $"{_options.Host}:{_options.Port}",
            Topic = _options.Topic,
            LastMessageOnUtc = lastMessageOnUtc ?? currentState.LastMessageOnUtc,
            LastError = lastError
        };
    }

    private static byte[] EncodeString(string? value)
    {
        var content = Encoding.UTF8.GetBytes(value ?? string.Empty);
        return [.. EncodeUInt16((ushort)content.Length), .. content];
    }

    private static byte[] EncodeUInt16(ushort value)
    {
        return [(byte)(value >> 8), (byte)(value & 0xFF)];
    }

    private static IEnumerable<byte> EncodeRemainingLength(int value)
    {
        do
        {
            var digit = value % 128;
            value /= 128;
            if (value > 0)
            {
                digit |= 0x80;
            }

            yield return (byte)digit;
        } while (value > 0);
    }

    private static async Task<byte> ReadByteAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = await ReadExactAsync(stream, 1, cancellationToken);
        return buffer[0];
    }

    private static async Task<int> ReadRemainingLengthAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var multiplier = 1;
        var value = 0;

        while (true)
        {
            var encodedByte = await ReadByteAsync(stream, cancellationToken);
            value += (encodedByte & 127) * multiplier;

            if ((encodedByte & 128) == 0)
            {
                return value;
            }

            multiplier *= 128;
            if (multiplier > 128 * 128 * 128)
            {
                throw new InvalidOperationException("MQTT remaining length is too large.");
            }
        }
    }

    private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int length, CancellationToken cancellationToken)
    {
        var buffer = new byte[length];
        var offset = 0;

        while (offset < length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (read == 0)
            {
                throw new IOException("MQTT broker closed the connection.");
            }

            offset += read;
        }

        return buffer;
    }

    private static ushort ReadUInt16(byte[] buffer, ref int position)
    {
        var value = (ushort)((buffer[position] << 8) | buffer[position + 1]);
        position += 2;
        return value;
    }

    private static string? NormalizeToken(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
#pragma warning restore CS1591
