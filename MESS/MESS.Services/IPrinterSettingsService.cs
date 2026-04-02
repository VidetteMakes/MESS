using MESS.Services.DTOs.PrinterSettings;

namespace MESS.Services.CRUD.PrinterSettings;

/// <summary>
/// Provides operations for managing printer settings.
/// </summary>
public interface IPrinterSettingsService
{
    /// <summary>
    /// Gets the current printer settings. Creates default settings if none exist.
    /// </summary>
    Task<PrinterSettingsDTO> GetSettingsAsync();

    /// <summary>
    /// Updates the printer settings.
    /// </summary>
    Task<PrinterSettingsDTO> UpdateSettingsAsync(PrinterSettingsDTO settings);

    /// <summary>
    /// Tests the connection to the configured network printer.
    /// </summary>
    /// <returns>True if connection successful, false otherwise.</returns>
    Task<bool> TestPrinterConnectionAsync();
}
