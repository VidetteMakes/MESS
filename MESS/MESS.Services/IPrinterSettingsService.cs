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

    /// <summary>
    /// Tests the connection to a specific network printer.
    /// </summary>
    /// <param name="printer">The printer to test.</param>
    /// <returns>True if connection successful, false otherwise.</returns>
    Task<bool> TestPrinterConnectionAsync(BrotherPrinterSettingsDTO printer);

    /// <summary>
    /// Attempts to print a text job through the configured Brother/network printers.
    /// </summary>
    /// <param name="jobName">The print job name.</param>
    /// <param name="content">The printable content.</param>
    /// <returns>True if a configured printer accepted the job; otherwise, false.</returns>
    Task<bool> TryPrintAsync(string jobName, string content);
}
