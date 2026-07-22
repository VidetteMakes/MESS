using MESS.Blazor.Components;
using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Data.Seed;
using MESS.Services.CRUD.Products;
using MESS.Services.CRUD.ApplicationUser;
using MESS.Services.CRUD.PartDefinitions;
using MESS.Services.CRUD.PartTraceability;
using MESS.Services.CRUD.Defects;
using MESS.Services.CRUD.ProductionLogs;
using MESS.Services.UI.LocalCacheManager;
using MESS.Services.UI.DarkMode;
using MESS.Services.CRUD.SerializableParts;
using MESS.Services.CRUD.Tags;
using MESS.Services.UI.SessionManager;
using MESS.Services.CRUD.WorkInstructions;
using MESS.Services.Files.ApplicationUsers;
using MESS.Services.Files.ProductionLogs;
using MESS.Services.Files.WorkInstructions;
using MESS.Services.Media.WorkInstructions;
using MESS.Services.UI.PartTraceability;
using MESS.Services.UI.ProductionLogEvent;
using MESS.Services.UI.QrCodes;
using MESS.Services.UI.ExportToken;
using MESS.Services.UI.WorkInstructionEditor;
using MESS.Services.UI.WorkInstructionImport;
using MESS.Services.Configuration;
using MESS.Blazor.Configuration;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<ApplicationContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MESSConnection");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Retry on transient failures (supported by Npgsql)
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

builder.Services.AddCascadingAuthenticationState();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
    
    
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductResolver, ProductResolver>();
builder.Services.AddScoped<IWorkInstructionService, WorkInstructionService>();
builder.Services.AddScoped<IWorkInstructionUpdater, WorkInstructionUpdater>();
builder.Services.AddScoped<IPartNodeResolver, PartNodeResolver>();
builder.Services.AddScoped<IPartDefinitionResolver, PartDefinitionResolver>();
builder.Services.AddScoped<IProductionLogService, ProductionLogService>();
builder.Services.AddScoped<IProductionLogExcelService, ProductionLogExcelService>();
builder.Services.AddScoped<IExportTokenService, ExportTokenService>();
builder.Services.AddScoped<IDefectCodeService, DefectCodeService>();
builder.Services.AddScoped<ILocalCacheManager, LocalCacheManager>();
builder.Services.AddScoped<ISessionManager, SessionManager>();
builder.Services.AddScoped<IApplicationUserService, ApplicationUserService>();
builder.Services.AddScoped<IApplicationUserFileService, ApplicationUserFileService>();
builder.Services.AddScoped<IPartDefinitionService, PartDefinitionService>();
builder.Services.AddScoped<IPartTraceabilityFormService, PartTraceabilityFormService>();
builder.Services.AddScoped<IPartTraceabilityPersistenceService, PartTraceabilityPersistenceService>();
builder.Services.AddScoped<IPartResolver, PartResolver>();
builder.Services.AddScoped<IPartTraceabilityReworkService, PartTraceabilityReworkService>();
builder.Services.AddScoped<ISerializablePartService, SerializablePartService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IProductionLogEventService, ProductionLogEventService>();
builder.Services.AddScoped<IWorkInstructionEditorService, WorkInstructionEditorService>();
builder.Services.AddScoped<IWorkInstructionFileService, WorkInstructionFileService>();
builder.Services.AddScoped<IWorkInstructionImportService, WorkInstructionImportService>();
builder.Services.AddScoped<IWorkInstructionImageService, WorkInstructionImageService>();
builder.Services.AddScoped<RoleInitializer>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddMudServices();

builder.Services.Configure<MessAuthOptions>(
    builder.Configuration.GetSection(MessAuthOptions.SectionName));

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password policy for accounts that opt in to password sign-in
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    
    // Username
    options.User.RequireUniqueEmail = false;
    
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
});

// Adding Services for Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationContext>()
    .AddDefaultTokenProviders();

if (MicrosoftEntraAuthConfiguration.TryGetMicrosoftEntraCredentials(
        builder.Configuration,
        out var tenantId,
        out var clientId,
        out var clientSecret))
{
    builder.Services.AddAuthentication()
        .AddOpenIdConnect("Microsoft", options =>
        {
            options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.CallbackPath = "/signin-microsoft";
            options.SaveTokens = true;
            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
        });
}

// Roles
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireTechnician", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("RequireOperator", policy =>
        policy.RequireRole("Operator"));
});

builder.Services.AddAntiforgery();

// Setup FluentUI
builder.Services.AddFluentUIComponents();

builder.Services.AddScoped<DarkModeInstance>();



var logLevel = builder.Environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Warning;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/MESS_Blazor_Error.logs", restrictedToMinimumLevel: LogEventLevel.Error, rollingInterval: RollingInterval.Month)
    .WriteTo.File("Logs/MESS_Blazor_Warning.logs", restrictedToMinimumLevel: LogEventLevel.Warning, rollingInterval: RollingInterval.Day)
    .WriteTo.File("Logs/MESS_Blazor_All.logs",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}}",
        rollingInterval: RollingInterval.Day)
    .MinimumLevel.Is(logLevel)
    .CreateLogger();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    // Initializes the roles if they are not already created in the database
    var roleInit = scope.ServiceProvider.GetRequiredService<RoleInitializer>();
    await roleInit.InitializeAsync();
    
    // Seed default technician
    await InitialUserSeed.SeedDefaultUserAsync(scope.ServiceProvider);
    
    // Seeds default data
    SeedWorkInstructions.Seed(scope.ServiceProvider);
}



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages();

app.MapControllers();

app.Run();