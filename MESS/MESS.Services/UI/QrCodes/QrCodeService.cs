using MESS.Services.CRUD.PrinterSettings;
using QRCoder;

namespace MESS.Services.UI.QrCodes;

using Microsoft.JSInterop;
using QRCoder;

/// <inheritdoc/>
public class QrCodeService : IQrCodeService
{
    private readonly IJSRuntime _js;
    private readonly IPrinterSettingsService _printerSettingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrCodeService"/> class.
    /// </summary>
    /// <param name="js">The <see cref="IJSRuntime"/> instance used to invoke JavaScript functions for printing QR codes.</param>
    /// <param name="printerSettingsService">The printer settings service used to attempt configured network printing.</param>
    /// <remarks>
    /// This constructor injects the JavaScript runtime dependency, allowing the service to call
    /// the client-side <c>printQRCode</c> function to render and print QR codes from Blazor components or other services.
    /// </remarks>
    public QrCodeService(IJSRuntime js, IPrinterSettingsService printerSettingsService)
    {
        _js = js;
        _printerSettingsService = printerSettingsService;
    }

    /// <inheritdoc/>
    public async Task PrintAsync(string content, string label)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        var settings = await _printerSettingsService.GetSettingsAsync();

        if (settings.PrintQrLabels)
        {
            var printed = await _printerSettingsService.TryPrintAsync(
                "MESS QR Label",
                $"{label}{Environment.NewLine}{content}");

            if (printed)
            {
                return;
            }

            if (!settings.AutoFallbackToBrowser)
            {
                throw new InvalidOperationException("Could not print QR label to any configured Brother printer.");
            }
        }

        // Generate QR
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new BitmapByteQRCode(qrCodeData);
        var qrBytes = qrCode.GetGraphic(20);

        var dataUrl = $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";

        // Call JS print
        await _js.InvokeVoidAsync("printQRCode", dataUrl, label);
    }
}
