using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.DTOs.PrinterSettings;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net.Sockets;

namespace MESS.Services.CRUD.PrinterSettings;

/// <inheritdoc />
public class PrinterSettingsService : IPrinterSettingsService
{
    private readonly IDbContextFactory<ApplicationContext> _contextFactory;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrinterSettingsService"/> class.
    /// </summary>
    public PrinterSettingsService(
        IDbContextFactory<ApplicationContext> contextFactory,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _contextFactory = contextFactory;
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <inheritdoc />
    public async Task<PrinterSettingsDTO> GetSettingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.PrinterSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (settings is null)
        {
            // Create default settings if none exist
            settings = new MESS.Data.Models.PrinterSettings
            {
                IsEnabled = false,
                PrinterType = "BrotherPTP700",
                IpAddress = null,
                Port = 9100,
                TimeoutMilliseconds = 5000,
                AutoFallbackToBrowser = true,
                LabelWidthMm = 36,
                LabelHeightMm = 23,
                PrintQrLabels = true,
                PrintRedTags = true,
                Notes = "Default configuration - please fill in printer details",
                CreatedBy = "system",
                LastModifiedBy = "system"
            };

            context.PrinterSettings.Add(settings);
            await context.SaveChangesAsync();

            Log.Information("Created default printer settings");
        }

        return MapToDTO(settings);
    }

    /// <inheritdoc />
    public async Task<PrinterSettingsDTO> UpdateSettingsAsync(PrinterSettingsDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var currentUser = await GetCurrentUserNameAsync();

        var settings = await context.PrinterSettings.FirstOrDefaultAsync();

        if (settings is null)
        {
            // Create if doesn't exist
            settings = new MESS.Data.Models.PrinterSettings
            {
                CreatedBy = currentUser,
            };
            context.PrinterSettings.Add(settings);
        }

        // Update fields
        settings.IsEnabled = dto.IsEnabled;
        settings.PrinterType = dto.PrinterType;
        settings.IpAddress = dto.IpAddress;
        settings.Port = dto.Port;
        settings.TimeoutMilliseconds = dto.TimeoutMilliseconds;
        settings.AutoFallbackToBrowser = dto.AutoFallbackToBrowser;
        settings.LabelWidthMm = dto.LabelWidthMm;
        settings.LabelHeightMm = dto.LabelHeightMm;
        settings.PrintQrLabels = dto.PrintQrLabels;
        settings.PrintRedTags = dto.PrintRedTags;
        settings.Notes = dto.Notes;
        settings.LastModifiedBy = currentUser;

        await context.SaveChangesAsync();

        Log.Information("Updated printer settings by user {UserName}", currentUser);

        return MapToDTO(settings);
    }

    /// <inheritdoc />
    public async Task<bool> TestPrinterConnectionAsync()
    {
        var settings = await GetSettingsAsync();

        if (!settings.IsEnabled || string.IsNullOrWhiteSpace(settings.IpAddress))
        {
            Log.Warning("Printer test failed: printer not enabled or IP address not configured");
            return false;
        }

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(settings.IpAddress, settings.Port);
            var completedTask = await Task.WhenAny(
                connectTask,
                Task.Delay(settings.TimeoutMilliseconds)
            );

            if (completedTask != connectTask)
            {
                Log.Warning("Printer connection test timed out after {Timeout}ms", settings.TimeoutMilliseconds);
                return false;
            }

            if (connectTask.IsCompletedSuccessfully)
            {
                Log.Information("Printer connection test successful to {IpAddress}:{Port}", settings.IpAddress, settings.Port);
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Printer connection test failed to {IpAddress}:{Port}", settings.IpAddress, settings.Port);
            return false;
        }

        return false;
    }

    private static PrinterSettingsDTO MapToDTO(MESS.Data.Models.PrinterSettings entity)
    {
        return new PrinterSettingsDTO
        {
            Id = entity.Id,
            IsEnabled = entity.IsEnabled,
            PrinterType = entity.PrinterType,
            IpAddress = entity.IpAddress,
            Port = entity.Port,
            TimeoutMilliseconds = entity.TimeoutMilliseconds,
            AutoFallbackToBrowser = entity.AutoFallbackToBrowser,
            LabelWidthMm = entity.LabelWidthMm,
            LabelHeightMm = entity.LabelHeightMm,
            PrintQrLabels = entity.PrintQrLabels,
            PrintRedTags = entity.PrintRedTags,
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
