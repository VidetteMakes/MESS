using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.DTOs.PrinterSettings;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text;
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
            .OrderBy(p => p.Id)
            .ToListAsync();

        if (settings.Count == 0)
        {
            // Create default settings if none exist
            var defaultSettings = new MESS.Data.Models.PrinterSettings
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

            context.PrinterSettings.Add(defaultSettings);
            await context.SaveChangesAsync();

            Log.Information("Created default printer settings");

            settings.Add(defaultSettings);
        }

        return MapToDTO(settings);
    }

    /// <inheritdoc />
    public async Task<PrinterSettingsDTO> UpdateSettingsAsync(PrinterSettingsDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var currentUser = await GetCurrentUserNameAsync();

        var requestedPrinters = dto.BrotherPrinters.Count > 0
            ? dto.BrotherPrinters
            : [MapLegacyPrinter(dto)];

        var existingPrinters = await context.PrinterSettings
            .OrderBy(p => p.Id)
            .ToListAsync();

        foreach (var requestedPrinter in requestedPrinters)
        {
            var settings = requestedPrinter.Id > 0
                ? existingPrinters.FirstOrDefault(p => p.Id == requestedPrinter.Id)
                : null;

            if (settings is null)
            {
                settings = new MESS.Data.Models.PrinterSettings
                {
                    CreatedBy = currentUser,
                };
                context.PrinterSettings.Add(settings);
            }

            settings.IsEnabled = requestedPrinter.IsEnabled;
            settings.PrinterType = requestedPrinter.PrinterType;
            settings.IpAddress = requestedPrinter.IpAddress?.Trim();
            settings.Port = requestedPrinter.Port;
            settings.TimeoutMilliseconds = dto.TimeoutMilliseconds;
            settings.AutoFallbackToBrowser = dto.AutoFallbackToBrowser;
            settings.LabelWidthMm = dto.LabelWidthMm;
            settings.LabelHeightMm = dto.LabelHeightMm;
            settings.PrintQrLabels = dto.PrintQrLabels;
            settings.PrintRedTags = dto.PrintRedTags;
            settings.Notes = requestedPrinter.Notes;
            settings.LastModifiedBy = currentUser;
        }

        var requestedIds = requestedPrinters
            .Where(p => p.Id > 0)
            .Select(p => p.Id)
            .ToHashSet();

        var removedPrinters = existingPrinters
            .Where(p => p.Id > 0 && !requestedIds.Contains(p.Id))
            .ToList();

        context.PrinterSettings.RemoveRange(removedPrinters);

        await context.SaveChangesAsync();

        Log.Information("Updated printer settings by user {UserName}", currentUser);

        return await GetSettingsAsync();
    }

    /// <inheritdoc />
    public async Task<bool> TestPrinterConnectionAsync()
    {
        var settings = await GetSettingsAsync();

        var printers = GetEnabledPrinters(settings).ToList();
        if (printers.Count == 0)
        {
            Log.Warning("Printer test failed: no enabled printer with an IP address is configured");
            return false;
        }

        foreach (var printer in printers)
        {
            if (await TestPrinterConnectionAsync(printer))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> TestPrinterConnectionAsync(BrotherPrinterSettingsDTO printer)
    {
        ArgumentNullException.ThrowIfNull(printer);

        if (!printer.IsEnabled || string.IsNullOrWhiteSpace(printer.IpAddress))
        {
            Log.Warning("Printer test failed: printer not enabled or IP address not configured");
            return false;
        }

        var settings = await GetSettingsAsync();

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(printer.IpAddress, printer.Port);
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
                Log.Information("Printer connection test successful to {IpAddress}:{Port}", printer.IpAddress, printer.Port);
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Printer connection test failed to {IpAddress}:{Port}", printer.IpAddress, printer.Port);
            return false;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> TryPrintAsync(string jobName, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var settings = await GetSettingsAsync();

        foreach (var printer in GetEnabledPrinters(settings))
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(printer.IpAddress!, printer.Port);
                var completedTask = await Task.WhenAny(connectTask, Task.Delay(settings.TimeoutMilliseconds));

                if (completedTask != connectTask || !connectTask.IsCompletedSuccessfully)
                {
                    Log.Warning(
                        "Network print job {JobName} could not connect to {IpAddress}:{Port}",
                        jobName,
                        printer.IpAddress,
                        printer.Port);
                    continue;
                }

                await using var stream = client.GetStream();
                var payload = BuildRawPrintPayload(jobName, content);
                await stream.WriteAsync(payload);
                await stream.FlushAsync();

                Log.Information(
                    "Network print job {JobName} sent to {IpAddress}:{Port}",
                    jobName,
                    printer.IpAddress,
                    printer.Port);

                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(
                    ex,
                    "Network print job {JobName} failed on {IpAddress}:{Port}",
                    jobName,
                    printer.IpAddress,
                    printer.Port);
            }
        }

        return false;
    }

    private static PrinterSettingsDTO MapToDTO(IReadOnlyList<MESS.Data.Models.PrinterSettings> entities)
    {
        var entity = entities.First();
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
            Notes = entity.Notes,
            BrotherPrinters = entities.Select(MapToBrotherPrinterDTO).ToList()
        };
    }

    private static BrotherPrinterSettingsDTO MapToBrotherPrinterDTO(MESS.Data.Models.PrinterSettings entity)
    {
        return new BrotherPrinterSettingsDTO
        {
            Id = entity.Id,
            IsEnabled = entity.IsEnabled,
            PrinterType = entity.PrinterType,
            IpAddress = entity.IpAddress,
            Port = entity.Port,
            Notes = entity.Notes
        };
    }

    private static BrotherPrinterSettingsDTO MapLegacyPrinter(PrinterSettingsDTO dto)
    {
        return new BrotherPrinterSettingsDTO
        {
            Id = dto.Id,
            IsEnabled = dto.IsEnabled,
            PrinterType = dto.PrinterType,
            IpAddress = dto.IpAddress,
            Port = dto.Port,
            Notes = dto.Notes
        };
    }

    private static IEnumerable<BrotherPrinterSettingsDTO> GetEnabledPrinters(PrinterSettingsDTO settings)
    {
        return settings.BrotherPrinters
            .Where(printer => printer.IsEnabled && !string.IsNullOrWhiteSpace(printer.IpAddress));
    }

    private static byte[] BuildRawPrintPayload(string jobName, string content)
    {
        var sanitizedJobName = string.IsNullOrWhiteSpace(jobName) ? "MESS Print Job" : jobName.Trim();
        var sanitizedContent = content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal);
        var text = $"\x1B@{sanitizedJobName}\r\n\r\n{sanitizedContent}\r\n\f";
        return Encoding.ASCII.GetBytes(text);
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
