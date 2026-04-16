using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.DTOs.DevelopmentBoardSettings;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net.Sockets;

namespace MESS.Services.CRUD.DevelopmentBoardSettings;

/// <inheritdoc />
public class DevelopmentBoardSettingsService : IDevelopmentBoardSettingsService
{
    private readonly IDbContextFactory<ApplicationContext> _contextFactory;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevelopmentBoardSettingsService"/> class.
    /// </summary>
    public DevelopmentBoardSettingsService(
        IDbContextFactory<ApplicationContext> contextFactory,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _contextFactory = contextFactory;
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <inheritdoc />
    public async Task<DevelopmentBoardSettingsDTO> GetSettingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.DevelopmentBoardSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (settings is null)
        {
            settings = new MESS.Data.Models.DevelopmentBoardSettings
            {
                IsEnabled = false,
                DeviceName = "ESPRESSIF ESP32-S3 DevKitC-1",
                ConnectionMode = "WiFi",
                HostAddress = null,
                Port = 80,
                ApiPath = "/api/device",
                WifiSsid = null,
                WifiPassword = null,
                SoftwareEndpoint = null,
                AccessToken = null,
                TimeoutMilliseconds = 5000,
                Notes = "Default configuration - add the board network details to connect it to MESS.",
                CreatedBy = "system",
                LastModifiedBy = "system"
            };

            context.DevelopmentBoardSettings.Add(settings);
            await context.SaveChangesAsync();

            Log.Information("Created default development board settings");
        }

        return MapToDTO(settings);
    }

    /// <inheritdoc />
    public async Task<DevelopmentBoardSettingsDTO> UpdateSettingsAsync(DevelopmentBoardSettingsDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var currentUser = await GetCurrentUserNameAsync();

        var settings = await context.DevelopmentBoardSettings.FirstOrDefaultAsync();

        if (settings is null)
        {
            settings = new MESS.Data.Models.DevelopmentBoardSettings
            {
                CreatedBy = currentUser
            };
            context.DevelopmentBoardSettings.Add(settings);
        }

        settings.IsEnabled = dto.IsEnabled;
        settings.DeviceName = dto.DeviceName;
        settings.ConnectionMode = dto.ConnectionMode;
        settings.HostAddress = dto.HostAddress;
        settings.Port = dto.Port;
        settings.ApiPath = dto.ApiPath;
        settings.WifiSsid = dto.WifiSsid;
        settings.WifiPassword = dto.WifiPassword;
        settings.SoftwareEndpoint = dto.SoftwareEndpoint;
        settings.AccessToken = dto.AccessToken;
        settings.TimeoutMilliseconds = dto.TimeoutMilliseconds;
        settings.Notes = dto.Notes;
        settings.LastModifiedBy = currentUser;

        await context.SaveChangesAsync();

        Log.Information("Updated development board settings by user {UserName}", currentUser);

        return MapToDTO(settings);
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync()
    {
        var settings = await GetSettingsAsync();

        if (!settings.IsEnabled || string.IsNullOrWhiteSpace(settings.HostAddress))
        {
            Log.Warning("Development board test failed: board not enabled or host address not configured");
            return false;
        }

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(settings.HostAddress, settings.Port);
            var completedTask = await Task.WhenAny(
                connectTask,
                Task.Delay(settings.TimeoutMilliseconds));

            if (completedTask != connectTask)
            {
                Log.Warning("Development board connection timed out after {Timeout}ms", settings.TimeoutMilliseconds);
                return false;
            }

            if (connectTask.IsCompletedSuccessfully)
            {
                Log.Information(
                    "Development board connection test successful to {HostAddress}:{Port}",
                    settings.HostAddress,
                    settings.Port);
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Development board connection test failed to {HostAddress}:{Port}",
                settings.HostAddress,
                settings.Port);
            return false;
        }

        return false;
    }

    private static DevelopmentBoardSettingsDTO MapToDTO(MESS.Data.Models.DevelopmentBoardSettings entity)
    {
        return new DevelopmentBoardSettingsDTO
        {
            Id = entity.Id,
            IsEnabled = entity.IsEnabled,
            DeviceName = entity.DeviceName,
            ConnectionMode = entity.ConnectionMode,
            HostAddress = entity.HostAddress,
            Port = entity.Port,
            ApiPath = entity.ApiPath,
            WifiSsid = entity.WifiSsid,
            WifiPassword = entity.WifiPassword,
            SoftwareEndpoint = entity.SoftwareEndpoint,
            AccessToken = entity.AccessToken,
            TimeoutMilliseconds = entity.TimeoutMilliseconds,
            Notes = entity.Notes
        };
    }

    private async Task<string> GetCurrentUserNameAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.Name?.Trim() switch
        {
            { Length: > 0 } userName => userName,
            _ => "system"
        };
    }
}
