using System.ComponentModel.DataAnnotations;

namespace MESS.Services.DTOs.DevelopmentBoardSettings;

/// <summary>
/// Data transfer object for development board connection settings.
/// </summary>
public class DevelopmentBoardSettingsDTO
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the board integration is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the board display name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceName { get; set; } = "ESPRESSIF ESP32-S3 DevKitC-1";

    /// <summary>
    /// Gets or sets the connection mode for the board.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string ConnectionMode { get; set; } = "WiFi";

    /// <summary>
    /// Gets or sets the board host address or hostname.
    /// </summary>
    [MaxLength(100)]
    public string? HostAddress { get; set; }

    /// <summary>
    /// Gets or sets the board network port.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 80;

    /// <summary>
    /// Gets or sets the API path exposed by the board.
    /// </summary>
    [MaxLength(150)]
    public string ApiPath { get; set; } = "/api/device";

    /// <summary>
    /// Gets or sets the Wi-Fi SSID used by the board.
    /// </summary>
    [MaxLength(100)]
    public string? WifiSsid { get; set; }

    /// <summary>
    /// Gets or sets the Wi-Fi password used by the board.
    /// </summary>
    [MaxLength(100)]
    public string? WifiPassword { get; set; }

    /// <summary>
    /// Gets or sets the MESS software endpoint used by the board.
    /// </summary>
    [MaxLength(200)]
    public string? SoftwareEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the shared access token or key.
    /// </summary>
    [MaxLength(150)]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds.
    /// </summary>
    [Range(100, 60000)]
    public int TimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Gets or sets notes for the board configuration.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
