using MESS.Tests.UI_Testing.Setup;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace MESS.Tests.UI_Testing.ProductionLog.EndToEnd;

public class ProductionLogCreationE2ETests : PageTest, IClassFixture<AuthFixture>
{
    private readonly AuthFixture _authFixture;

    public ProductionLogCreationE2ETests(AuthFixture authFixture)
    {
        _authFixture = authFixture;
    }
    
    [Fact]
    public async Task CreateProductionLog_AsOperator_SuccessfullyCreatesProductionLog()
    {
        var context = await Browser.NewContextAsync(new()
        {
            StorageStatePath = _authFixture.StorageStatePath,
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync("https://localhost:7152/production-log");
        
        // Select Product
        await page.SelectOptionAsync("#product-select", new []{ "ABC Controller" });
        
        // Select Work Instruction
        await page.SelectOptionAsync("#workInstruction-select", new []{"ABC Subassembly"});
        
        // Enter Steps
        await Expect(page.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(3);

        var rowLocator = page.GetByRole(AriaRole.Listitem);

        await rowLocator
            .Filter(new LocatorFilterOptions
            {
                Has = page.GetByRole(AriaRole.Radio, new PageGetByRoleOptions
                {
                    Name = "Success"
                })
            })
            .Nth(0)
            .ClickAsync();
        
        await rowLocator
            .Filter(new LocatorFilterOptions
            {
                Has = page.GetByRole(AriaRole.Radio, new PageGetByRoleOptions
                {
                    Name = "Failure"
                })
            })
            .Nth(1)
            .ClickAsync();
        
        // Input text into notes field
        
        // Submit
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions()
        {
            Name = "Submit Log"
        }).ClickAsync();

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions()
        {
            Name = "Submit",
            Exact = true
        }).ClickAsync();
        
        // Validate
        
        
        
        await page.CloseAsync();
        await context.CloseAsync();
    }

    [Fact]
    public async Task CreateProductionLog_PartialPartNumberValues_CorrectlyShowsConfirmationAlert()
    {
        var context = await Browser.NewContextAsync(new()
        {
            StorageStatePath = _authFixture.StorageStatePath,
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        
        await page.GotoAsync("https://localhost:7152/production-log");
        
        await page.Locator("#product-select").SelectOptionAsync(new[] { "ABC Controller" });
        await page.SelectOptionAsync("#workInstruction-select", new []{ new SelectOptionValue { Index = 1} });
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Product Serial Number:" }).ClickAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Product Serial Number:" }).FillAsync("000");
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Display Board" }).ClickAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Display Board" }).FillAsync("000");
        await page.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "Test display and Humidity Sensor." }).Locator("label").Nth(1).ClickAsync();
        await page.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "Attach the Display" }).Locator("label").Nth(1).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Submit Log" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Article)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateProductionLogPage_Logout_CorrectlyRedirectsToLoginPage()
    {
        var context = await Browser.NewContextAsync(new()
        {
            StorageStatePath = _authFixture.StorageStatePath,
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        
        await page.GotoAsync("https://localhost:7152/production-log");
        await page.GetByRole(AriaRole.Button, new() { Name = "Logout" }).ClickAsync();
        await Expect(page.Locator("div").Filter(new() { HasText = "Email: Login" }).Nth(3)).ToBeVisibleAsync();
    }
}