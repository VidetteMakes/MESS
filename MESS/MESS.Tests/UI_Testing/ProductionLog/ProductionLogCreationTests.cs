using System.Security.Claims;
using Bunit;
using MESS.Blazor.Components.Pages.ProductionLog;
using MESS.Services.CRUD.ApplicationUser;
using MESS.Services.CRUD.PartTraceability;
using MESS.Services.CRUD.Products;
using MESS.Services.CRUD.ProductionLogs;
using MESS.Services.CRUD.WorkInstructions;
using MESS.Services.DTOs.ProductionLogs.Cache;
using MESS.Services.CRUD.ProductionLogParts;
using MESS.Services.DTOs.ProductionLogs.CreateRequest;
using MESS.Services.UI.SessionManager;
using MESS.Services.UI.LocalCacheManager;
using MESS.Services.UI.PartTraceability;
using MESS.Services.UI.ProductionLogEvent;
using MESS.Services.UI.QrCodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Moq;
using Xunit.Abstractions;

namespace MESS.Tests.UI_Testing.ProductionLog;

public class ProductionLogCreationTests : BunitContext
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IProductionLogService> _productionLogServiceMock;
    private readonly Mock<IWorkInstructionService> _workInstructionServiceMock;
    private readonly Mock<IProductService> _productServiceMock;
    private readonly Mock<IApplicationUserService> _userServiceMock;
    private readonly Mock<IPartTraceabilityPersistenceService> _partTraceabilityPersistenceServiceMock;
    private readonly Mock<IPartTraceabilityStateService> _partTraceabilityServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;

    private readonly Mock<ILocalCacheManager> _localCacheManagerMock;
    private readonly Mock<IProductionLogPartService> _serializationServiceMock;
    private readonly Mock<IProductionLogEventService> _productionLogEventServiceMock;
    private readonly Mock<AuthenticationStateProvider> _authProviderMock;
    private readonly Mock<ISessionManager> _sessionManagerMock;
    private readonly Mock<IJSRuntime> _jsRuntimeMock;
    private readonly Mock<IJSObjectReference> _jsModuleMock;
    private readonly Mock<IToastService> _toastServiceMock;
    private readonly Mock<IQrCodeService> _qrCodeServiceMock;
    

    public ProductionLogCreationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _productionLogServiceMock = new Mock<IProductionLogService>();
        _workInstructionServiceMock = new Mock<IWorkInstructionService>();
        _productServiceMock = new Mock<IProductService>();
        _userServiceMock = new Mock<IApplicationUserService>();
        _localCacheManagerMock = new Mock<ILocalCacheManager>();
        _serializationServiceMock = new Mock<IProductionLogPartService>();
        _productionLogEventServiceMock = new Mock<IProductionLogEventService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _authProviderMock = new Mock<AuthenticationStateProvider>();
        _sessionManagerMock = new Mock<ISessionManager>();
        _jsRuntimeMock = new Mock<IJSRuntime>();
        _jsModuleMock = new Mock<IJSObjectReference>();
        _toastServiceMock = new Mock<IToastService>();
        _qrCodeServiceMock = new Mock<IQrCodeService>();
        _partTraceabilityPersistenceServiceMock = new Mock<IPartTraceabilityPersistenceService>();
        _partTraceabilityServiceMock = new Mock<IPartTraceabilityStateService>();
            
        Services.AddSingleton(_productionLogServiceMock.Object);
        Services.AddSingleton(_workInstructionServiceMock.Object);
        Services.AddSingleton(_productServiceMock.Object);
        Services.AddSingleton(_userServiceMock.Object);
        Services.AddSingleton(_localCacheManagerMock.Object);
        Services.AddSingleton(_serializationServiceMock.Object);
        Services.AddSingleton(_productionLogEventServiceMock.Object);
        Services.AddSingleton(_dialogServiceMock.Object);
        Services.AddSingleton<AuthenticationStateProvider>(_authProviderMock.Object);
        Services.AddSingleton(_sessionManagerMock.Object);
        Services.AddSingleton(_jsRuntimeMock.Object);
        Services.AddSingleton(_toastServiceMock.Object);
        Services.AddSingleton(_qrCodeServiceMock.Object);
        Services.AddSingleton(_partTraceabilityPersistenceServiceMock.Object);
        Services.AddSingleton(_partTraceabilityServiceMock.Object);
            
        _jsRuntimeMock.Setup(js => js.InvokeAsync<IJSObjectReference>(
            "import", It.IsAny<object[]>())).ReturnsAsync(_jsModuleMock.Object);
            
        SetupAuthenticationState();
        SetupCacheDefaults();
        SetupAuthorizationServices();
    }
    
    private void SetupAuthenticationState()
    {
        var authState = new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.NameIdentifier, "user123")
            ], "testauth")));

        _authProviderMock.Setup(p => p.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);
    }

    private void SetupAuthorizationServices()
    {
        // Add required authorization services
        Services.AddAuthorization();
        Services.AddSingleton<IAuthorizationPolicyProvider>(new DefaultAuthorizationPolicyProvider(
            Options.Create(new AuthorizationOptions())));
        Services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();
    }

    private void SetupCacheDefaults()
    {
        _localCacheManagerMock.Setup(m => m.GetProductionLogFormAsync())
            .ReturnsAsync(new ProductionLogCacheDTO());

        _localCacheManagerMock.Setup(m => m.GetWorkflowActiveStatusAsync())
            .ReturnsAsync(false);
    }

    [Fact] 
    public async Task Create_OnlyWorkInstructionSelected_DoesNotCreateLog()
    {
        // Arrange
        var authContext = this.AddAuthorization();
        authContext.SetAuthorized("TechnicianUser");
        authContext.SetRoles("Technician");

        _localCacheManagerMock.Setup(m => m.GetProductionLogBatchAsync())
            .ReturnsAsync([
                new ProductionLogCacheDTO
                {
                    ProductionLogId = 1,
                    LogSteps = []
                }
            ]);

        // Render the component
        var cut = Render<Create>();

        // Find the EditForm component
        var editForm = cut.FindComponent<EditForm>();

        // Invoke the OnValidSubmit event callback manually
        await cut.InvokeAsync(async () => await editForm.Instance.OnValidSubmit.InvokeAsync());

        // Assert that CreateAsync was never called
        _productionLogServiceMock.Verify(
            service => service.CreateAsync(It.IsAny<ProductionLogCreateRequest>()),
            Times.Never);
    }
}