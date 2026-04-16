using Microsoft.Playwright;
using System.Threading.Tasks;

namespace MESS.Tests.UI_Testing.Setup;

public class AuthFixture : IAsyncLifetime
{
    public string StorageStatePath { get; } = "../../../UI_Testing/Playwright/.auth/state.json";

    public async ValueTask InitializeAsync()
    {
        // Setup browser and authenticate
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync("https://localhost:7152");
        await page.GetByPlaceholder("Search or select an operator...").FillAsync("technician@mess.com");
        await page.Keyboard.PressAsync("Tab");
        await page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();

        // Create storage path if not already available
        var directoryPath = Path.GetDirectoryName(StorageStatePath);

        if (!Directory.Exists(directoryPath))
        {
            if (directoryPath != null)
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        
        await context.StorageStateAsync(new()
        {
            Path = StorageStatePath
        });
    }

    public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);
}