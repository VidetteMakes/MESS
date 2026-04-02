namespace MESS.Services.DTOs.PrinterSettings;

/// <summary>
/// Data Transfer Object for printer settings configuration.
/// Used for displaying and updating printer settings in the UI.
/// </summary>
public class PrinterSettingsDTO
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether network printer is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the type of printer.
    /// </summary>
    public string PrinterType { get; set; } = "BrotherPTP700";

    /// <summary>
    /// Gets or sets the IP address of the printer.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the network port.
    /// </summary>
    public int Port { get; set; } = 9100;

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to fallback to browser print on failure.
    /// </summary>
    public bool AutoFallbackToBrowser { get; set; } = true;

    /// <summary>
    /// Gets or sets the label width in millimeters.
    /// </summary>
    public int LabelWidthMm { get; set; } = 36;

    /// <summary>
    /// Gets or sets the label height in millimeters.
    /// </summary>
    public int LabelHeightMm { get; set; } = 23;

    /// <summary>
    /// Gets or sets whether to print QR labels directly.
    /// </summary>
    public bool PrintQrLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to print red tags directly.
    /// </summary>
    public bool PrintRedTags { get; set; } = true;

    /// <summary>
    /// Gets or sets notes about the printer.
    /// </summary>
    public string? Notes { get; set; }
}
