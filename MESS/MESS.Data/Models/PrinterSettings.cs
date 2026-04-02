using System.ComponentModel.DataAnnotations;

namespace MESS.Data.Models;

/// <summary>
/// Represents network printer configuration settings for direct printing from MESS.
/// Allows administrators to configure Brother label printer or other network printers.
/// </summary>
public class PrinterSettings : AuditableEntity
{
    /// <summary>
    /// Gets or sets the primary key. Typically only one row exists in this table.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether network printer is enabled.
    /// When false, printing will fall back to browser print dialog.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the type of printer (e.g., "BrotherPTP700", "Generic").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PrinterType { get; set; } = "BrotherPTP700";

    /// <summary>
    /// Gets or sets the IP address of the network printer.
    /// Example: "192.168.1.100"
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the network port for the printer connection.
    /// Standard Brother port is 9100 for raw printing.
    /// </summary>
    public int Port { get; set; } = 9100;

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically fall back to browser print
    /// if network printer connection fails.
    /// </summary>
    public bool AutoFallbackToBrowser { get; set; } = true;

    /// <summary>
    /// Gets or sets the label width in millimeters.
    /// Common values: 12, 24, 36 for Brother TZe tapes.
    /// </summary>
    public int LabelWidthMm { get; set; } = 36;

    /// <summary>
    /// Gets or sets the label height in millimeters.
    /// </summary>
    public int LabelHeightMm { get; set; } = 23;

    /// <summary>
    /// Gets or sets a value indicating whether QR code labels should be printed directly.
    /// </summary>
    public bool PrintQrLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether red tag labels should be printed directly.
    /// </summary>
    public bool PrintRedTags { get; set; } = true;

    /// <summary>
    /// Gets or sets a note or description about the printer configuration.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
