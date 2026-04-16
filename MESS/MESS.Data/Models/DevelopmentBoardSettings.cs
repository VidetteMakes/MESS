using System.ComponentModel.DataAnnotations;

namespace MESS.Data.Models;

/// <summary>
/// Represents saved network and integration settings for the ESP32-S3 development board.
/// </summary>
public class DevelopmentBoardSettings : AuditableEntity
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether board integration is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the display name for the board.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceName { get; set; } = "ESPRESSIF ESP32-S3 DevKitC-1";

    /// <summary>
    /// Gets or sets the preferred connection mode.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string ConnectionMode { get; set; } = "WiFi";

    /// <summary>
    /// Gets or sets the board IP address or hostname.
    /// </summary>
    [MaxLength(100)]
    public string? HostAddress { get; set; }

    /// <summary>
    /// Gets or sets the board network port.
    /// </summary>
    public int Port { get; set; } = 80;

    /// <summary>
    /// Gets or sets the path used by the software to reach the board API.
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
    /// Gets or sets the software endpoint exposed to the board.
    /// </summary>
    [MaxLength(200)]
    public string? SoftwareEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the shared token for software-to-board communication.
    /// </summary>
    [MaxLength(150)]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the network timeout in milliseconds.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Gets or sets free-form notes for this board configuration.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
