using MESS.Services.DTOs.DevelopmentBoardSettings;

namespace MESS.Services.CRUD.DevelopmentBoardSettings;

/// <summary>
/// Provides operations for managing development board settings.
/// </summary>
public interface IDevelopmentBoardSettingsService
{
    /// <summary>
    /// Gets the current board settings. Creates defaults if none exist.
    /// </summary>
    Task<DevelopmentBoardSettingsDTO> GetSettingsAsync();

    /// <summary>
    /// Updates the board settings.
    /// </summary>
    Task<DevelopmentBoardSettingsDTO> UpdateSettingsAsync(DevelopmentBoardSettingsDTO settings);

    /// <summary>
    /// Tests connectivity to the configured board host and port.
    /// </summary>
    Task<bool> TestConnectionAsync();
}
