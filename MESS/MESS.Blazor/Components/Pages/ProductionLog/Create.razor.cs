using MESS.Blazor.Components.Dialogs;
using MESS.Services.DTOs.ProductionLogs.Cache;
using MESS.Services.DTOs.ProductionLogs.Form;
using MESS.Services.DTOs.ProductionLogs.LogSteps.Form;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
namespace MESS.Blazor.Components.Pages.ProductionLog;

using Data.Models;

internal enum Status
{
    NotStarted,
    InProgress,
    Completed
}
/// <summary>
/// Represents the Create component for managing production logs.
/// This component handles the creation, modification, and submission of production logs.
/// </summary>
public partial class Create : ComponentBase, IAsyncDisposable
{
    private const string Title = "Production Log";
    private bool IsLoading { get; set; } = true;
    private ConfirmationModal? popupRef;
    private MessageModal? errorPopupRef;
    private bool IsWorkflowActive { get; set; }
    private Status WorkInstructionStatus { get; set; } = Status.NotStarted;
    private bool IsSaved { get; set; }
    
    private Product? ActiveProduct { get; set; }
    private WorkInstruction? ActiveWorkInstruction { get; set; }

    /// <summary>
    /// Represents the current production logs being created or modified.
    /// The object in this collection hold all the details of the production log.
    /// </summary>
    protected ProductionLogBatch ProductionLogBatch = new();
    
    private List<Product>? Products { get; set; }
    private List<WorkInstruction>? WorkInstructions { get; set; }
    private List<WorkInstruction>? ActiveProductWorkInstructionList { get; set; }
    
    private string? ActiveLineOperator { get; set; }
    private string? ProductSerialNumber { get; set; }
    
    private IJSObjectReference? scrollToModule;
    
    private Func<List<ProductionLogFormDTO>, Task>? _autoSaveHandler;
    
    private int BatchSize { get; set; }
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        ProductionLogEventService.DisableAutoSave();
        await ProductionLogEventService.StopDbSaveTimerAsync();
        await LoadProducts();
        await GetInProgressAsync();
        BatchSize = await LocalCacheManager.GetBatchSizeAsync();

         // Load cached forms as a list of ProductionLog
        ProductionLogBatch.Logs = await LoadCachedFormsAsync();

        bool cachedFormsLoaded = ProductionLogBatch.Logs.Count > 0;

        if (cachedFormsLoaded)
        {
            ProductionLogEventService.EnableAutoSave();
            await InvokeAsync(StateHasChanged);
        }
        
        ProductionLogEventService.StartDbSaveTimer();

        await ProductionLogEventService.SetCurrentProductionLogs(ProductionLogBatch.Logs);

        // AutoSave Trigger
        _autoSaveHandler = async logs =>
        {
            if (logs.Count == 0)
            {
                Log.Warning("Attempted to autosave an empty or null production log list.");
                return;
            }

            await LocalCacheManager.SetProductionLogBatchAsync(logs);

            await InvokeAsync(() =>
            {
                IsSaved = true;
                StateHasChanged();
            });
        };
        
        var result = await AuthProvider.GetAuthenticationStateAsync();
        ActiveLineOperator = result.User.Identity?.Name;
        // This must come before the LoadCachedForm method since if it finds a cached form, it will set the status to InProgress
        WorkInstructionStatus = Status.NotStarted;
        
        ProductionLogEventService.AutoSaveTriggered += _autoSaveHandler;
        
        // Register periodic database save handler
        ProductionLogEventService.DbSaveTriggered += SaveLogsToDatabaseHandler;
        
        ProductSerialNumber = ProductionLogPartService.CurrentProductNumber;
        
        ProductionLogPartService.CurrentProductNumberChanged += HandleProductNumberChanged;
        
        IsLoading = false;
    }
    
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            scrollToModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import",
                "./Scripts/ScrollTo.js");
        }
    }
    
    private async Task<List<ProductionLogFormDTO>> LoadCachedFormsAsync()
    {
        var cachedFormsData = await LocalCacheManager.GetProductionLogBatchAsync();

        if (cachedFormsData.Count == 0)
        {
            AddProductionLogs(BatchSize); // still initializes empty DTOs
            return ProductionLogBatch.Logs;
        }

        // Map cache DTOs into form DTOs
        var productionLogs = cachedFormsData
            .Select(cachedForm => cachedForm.ToFormDTO())
            .ToList();

        // Update status
        foreach (var log in productionLogs)
        {
            if (log.LogSteps.All(step => step.Attempts.Any(a => a.SubmitTime != DateTimeOffset.MinValue)))
            {
                WorkInstructionStatus = Status.Completed;
            }
            else
            {
                WorkInstructionStatus = Status.InProgress;
            }
        }

        return productionLogs;
    }
    
    private async Task SetActiveWorkInstruction(int workInstructionId)
    {
        if (workInstructionId <= 0)
        {
            ActiveWorkInstruction = null;
            await SetSelectedWorkInstructionId(null);
            ProductionLogEventService.SetCurrentWorkInstructionName(string.Empty);

            PartTraceabilityService.Clear();
            return;
        }

        if (ActiveProductWorkInstructionList != null)
        {
            var workInstruction = await WorkInstructionService.GetByIdAsync(workInstructionId);
            if (workInstruction?.Products == null)
                return;

            // Clear all related cached and in-memory data
            await LocalCacheManager.ClearProductionLogBatchAsync();
            ProductionLogBatch.Logs.Clear();
            await ProductionLogEventService.SetCurrentProductionLogs([]);
            PartTraceabilityService.Clear();

            // Set the new work instruction
            ActiveWorkInstruction = workInstruction;
            ProductionLogEventService.SetCurrentWorkInstructionName(workInstruction.Title);
            await LocalCacheManager.SetActiveWorkInstructionIdAsync(workInstruction.Id);
            await SetSelectedWorkInstructionId(workInstructionId);
            ProductionLogEventService.MarkClean();

            // Add new logs for the new instruction
            AddProductionLogs(BatchSize);
        }
    }

    private async Task SetActiveProduct(int productId)
    {
        if (Products == null)
            return;

        if (productId < 0)
        {
            ActiveWorkInstruction = null;
            ActiveProductWorkInstructionList = null;

            await LocalCacheManager.ClearProductionLogBatchAsync();
            ProductionLogBatch.Logs.Clear();
            await ProductionLogEventService.SetCurrentProductionLogs([]);
            PartTraceabilityService.Clear();

            await SetActiveWorkInstruction(-1);
            await LocalCacheManager.SetActiveProductAsync(null);
            return;
        }

        var product = Products.FirstOrDefault(p => p.Id == productId);
        if (product?.WorkInstructions == null)
            return;

        // Clear all related cached and in-memory data
        await LocalCacheManager.ClearProductionLogBatchAsync();
        ProductionLogBatch.Logs.Clear();
        await ProductionLogEventService.SetCurrentProductionLogs([]);
        PartTraceabilityService.Clear();

        // Proceed with setting new state
        ActiveProduct = product;
        ActiveProductWorkInstructionList = product.WorkInstructions.Where(w => w.IsActive).ToList();
        ProductionLogEventService.SetCurrentProductName(product.PartDefinition.Name);
        ProductionLogEventService.MarkClean();
        
        await SetActiveWorkInstruction(-1);
        await LocalCacheManager.SetActiveProductAsync(product);
    }

    private async Task GetCachedActiveProductAsync()
    {
        var result = await LocalCacheManager.GetActiveProductAsync();
        ActiveProduct = Products?.FirstOrDefault(p => p.PartDefinition.Name == result.Name);

        if (ActiveProduct == null) 
        {
            return;
        }
        
        ActiveProductWorkInstructionList = ActiveProduct.WorkInstructions?
            .Where(w => w.IsActive)
            .ToList() ?? new List<WorkInstruction>();
        
        ProductionLogEventService.SetCurrentProductName(ActiveProduct.PartDefinition.Name);
    }
    
    private async Task OnBatchSizeChanged(int newSize)
    {
        if (newSize < 1)
            newSize = 1;

        BatchSize = newSize;
        await LocalCacheManager.SetBatchSizeAsync(BatchSize);

        var currentCount = ProductionLogBatch.Logs.Count;

        if (newSize > currentCount)
        {
            // Add new logs
            var logsToAdd = newSize - currentCount;
            AddProductionLogs(logsToAdd);
        }
        else if (newSize < currentCount)
        {
            // Remove excess logs
            var removedCount = currentCount - newSize;

            // Remove logs from the batch
            ProductionLogBatch.Logs.RemoveRange(newSize, removedCount);

            // Remove matching traceability entries
            for (var index = newSize; index < currentCount; index++)
            {
                PartTraceabilityService.RemoveLog(index);
            }

            await ProductionLogEventService.SetCurrentProductionLogs(ProductionLogBatch.Logs);
        }
    }

    /// Sets the local storage variable
    private async Task SetInProgressAsync(bool isActive)
    {
        await LocalCacheManager.SetIsWorkflowActiveAsync(isActive);
        IsWorkflowActive = isActive;
    }

    private async Task GetInProgressAsync()
    {
        var result = await LocalCacheManager.GetWorkflowActiveStatusAsync();
        
        // If the result is true, then the operator was previously in the middle of a workflow
        if (result)
        {
            IsWorkflowActive = result;
            await GetCachedActiveWorkInstructionAsync();
            await GetCachedActiveProductAsync();
            return;
        }

        await SetInProgressAsync(false);
    }
    

    private async Task GetCachedActiveWorkInstructionAsync()
    {
        var result = await LocalCacheManager.GetActiveWorkInstructionIdAsync(); 
        await LoadActiveWorkInstruction(result);
    }

    private async Task LoadProducts()
    {
        try
        {
            var productsAsync = await ProductService.GetAllAsync();
            Products = productsAsync.ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading products for the production log Create view");
        }
    }
    
    private async Task SetSelectedWorkInstructionId(int? value)
    {
        if (value.HasValue)
        {
            await SetInProgressAsync(true);
        }
        await LocalCacheManager.SetActiveWorkInstructionIdAsync(value ?? -1);
    }
    
    private async Task LoadActiveWorkInstruction(int id)
    {
        ActiveWorkInstruction = await WorkInstructionService.GetByIdAsync(id);
        if (ActiveWorkInstruction == null)
        {
            return;
        }
        ProductionLogEventService.SetCurrentWorkInstructionName(ActiveWorkInstruction.Title);
    }

    /// <summary>
    /// Handles the submission of the production log. 
    /// Validates if all required parts have serial numbers before proceeding.
    /// If parts are missing serial numbers, prompts the user for confirmation.
    /// </summary>
    /// <remarks>
    /// - If `ActiveWorkInstruction` is null, the method exits early.
    /// - Calculates the total number of parts needed based on the active work instruction.
    /// - Compares the count of serial numbers logged with the total parts needed.
    /// - If all parts have serial numbers, proceeds to complete the submission.
    /// - Otherwise, displays a confirmation modal to the user.
    /// </remarks>
    /// <returns>An asynchronous operation.</returns>
    protected async Task HandleSubmit()
    {
        if (ActiveWorkInstruction == null)
        {
            return;
        }
        
        // Stop submission if there are unresolved tags
        if (await PartTraceabilityService.HasUnresolvedTagsAsync())
        {
            errorPopupRef?.Show(
                "One or more tag codes could not be resolved. Please fix these before submitting.",
                "Invalid Tags");
            return;
        }
        
        var partNodes = ActiveWorkInstruction.Nodes.Where(node => node.NodeType == WorkInstructionNodeType.Part);

        var totalPartsNeeded = 0;
        foreach (var node in partNodes)
        {
            // Cast to PartNode to access its Parts collection
            if (node is PartNode partNode)
            {
                totalPartsNeeded++;
            }
        }
        
        var totalPartsLogged = PartTraceabilityService.GetTotalPartsLogged();
        
        var allStepsHavePartsNeeded = totalPartsLogged >= totalPartsNeeded;

        if (!allStepsHavePartsNeeded)
        {
            popupRef?.Show("There are empty part entries. Are you sure you want to submit this log?");
        }
        else
        {
            await CompleteSubmit();
        }
    }

    private async Task HandleConfirmation(bool confirmed)
    {
        if (confirmed)
        {
            await CompleteSubmit();
        }
        
        popupRef?.Close();
    }
    
    private async Task SaveLogsToDatabaseHandler(List<ProductionLogFormDTO> logs)
    {
        await SaveLogsToDatabase();
    }

    private async Task SaveLogsToDatabase()
    {
        var authState = await AuthProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var userName = authState.User.Identity?.Name ?? string.Empty;

        // Collect all current logs from the event service (already FormDTOs now)
        var formDtos = ProductionLogEventService.CurrentProductionLogs;

        if (ActiveProduct is null || ActiveWorkInstruction is null)
        {
            // Show error message, redirect, or just return
            throw new InvalidOperationException("Product and work instruction must be selected before saving logs.");
        }
        
        // Stamp batch size onto each form DTO before saving
        foreach (var dto in formDtos)
        {
            dto.FromBatchOf = BatchSize;
        }
        
        // Call the service — it now takes care of metadata and persistence
        var result = await ProductionLogService.SaveOrUpdateBatchAsync(
            formDtos,
            createdBy: userName,
            operatorId: userId,
            productId: ActiveProduct.Id,
            workInstructionId: ActiveWorkInstruction.Id
        );

        //update in-memory IDs for any newly created logs so the UI has them
        foreach (var (formDto, createdId) in formDtos.Zip(result.CreatedIds))
        {
            if (formDto.Id == 0)
                formDto.Id = createdId;
        }
    }

    private async Task CompleteSubmit()
    {
        await SaveLogsToDatabase();
        
        for (var i = 0; i < ProductionLogEventService.CurrentProductionLogs.Count; i++)
        {
            var productionLog = ProductionLogEventService.CurrentProductionLogs[i];
            
            if (ActiveWorkInstruction is { ShouldGenerateQrCode: true })
            {
                await PrintQrCode(productionLog.Id, i);
            }

            ToastService.ShowSuccess("Successfully Created Production Log", 3000);
            
            // Add the new log to the session
            await SessionManager.AddProductionLogAsync(productionLog.Id);
        }
        
        // --- Part Traceability Persistence ---
        // 1. Map UI log index → DB ProductionLogId
        var logIndexToProductionLogId = ProductionLogEventService.CurrentProductionLogs
            .Select((log, index) => new { index, log.Id })
            .ToDictionary(x => x.index, x => x.Id);

        // 2. Create snapshots from UI state
        var snapshots = logIndexToProductionLogId.Keys
            .Select(logIndex => PartTraceabilityService.CreateSnapshot(logIndex))
            .ToList();

        // 3. Build operations
        var operations = PartTraceabilityPersistenceService.BuildOperations(
            snapshots,
            logIndexToProductionLogId);

        // 4. Persist operations
        try
        {
            foreach (var operation in operations)
            {
                await PartTraceabilityPersistenceService.PersistOperationBatchedAsync(operation);
            }
        }
        catch (Exception)
        {
            ToastService.ShowError("Failed to persist part traceability data.");
            throw;
        }
        
        // Reset the local storage values
        await LocalCacheManager.ClearProductionLogBatchAsync();
        
        await ResetFormState();
    }
    
    private void HandleProductNumberChanged()
    {
        ProductSerialNumber = ProductionLogPartService.CurrentProductNumber;

        InvokeAsync(StateHasChanged);
    }
    
    private async Task PrintQrCode(int productionLogId, int index)
    {
        // Just one line—service handles generation + JS interop
        await QrPrinter.PrintAsync(productionLogId.ToString(), $"#{index + 1}");
    }

    private async Task ResetFormState()
    {
        // Clear the in-memory part tracking
        PartTraceabilityService.Clear();
        
        // Reset the ProductionLogBatch Object
        ProductionLogBatch = new ProductionLogBatch();
        
        // Reinitialize the form with the current batch size
        AddProductionLogs(BatchSize);

        // Notify the event service with the empty list
        await ProductionLogEventService.SetCurrentProductionLogs(ProductionLogBatch.Logs);
        
        ProductionLogEventService.EnableAutoSave();
        WorkInstructionStatus = Status.NotStarted;
        ProductionLogEventService.MarkClean();
        
        if (scrollToModule != null)
        {
            await scrollToModule.InvokeVoidAsync("ScrollToTop");
        }
    }
    
    private async Task OnStepCompleted(List<LogStepFormDTO> productionLogSteps, bool? success)
    {
        if (ActiveWorkInstruction == null)
            return;
        
        await ProductionLogEventService.SetCurrentProductionLogs(ProductionLogBatch.Logs);
            
        var currentStatus = await GetWorkInstructionStatus();
        WorkInstructionStatus = currentStatus ? Status.Completed : Status.InProgress;
        
        var step = productionLogSteps.FirstOrDefault();
        
        if (step == null)
            return;
        
        // Find the current node that corresponds to this step
        var currentNode = ActiveWorkInstruction.Nodes.FirstOrDefault(n => n.Id == step.WorkInstructionStepId);

        if (currentNode != null)
        {
            // Sort nodes by Position to maintain the correct sequence
            var orderedNodes = ActiveWorkInstruction.Nodes
                .OrderBy(n => n.Position)
                .ToList();

            var currentIndex = orderedNodes.FindIndex(n => n.Id == currentNode.Id);

            if (currentIndex >= 0 && currentIndex < orderedNodes.Count - 1)
            {
                var nextStep = orderedNodes[currentIndex + 1];

                // If a step completed successfully, scroll to the next work instruction node
                if (success == true)
                {
                    string elementId = $"step-{nextStep.Position}";

                    if (scrollToModule != null)
                    {
                        await scrollToModule.InvokeVoidAsync("scrollTo", elementId);
                    }
                } 
            }
            
            // Scroll to the submit button if it's the last step and it was successful
            if (currentIndex == orderedNodes.Count - 1 && success == true)
            {
                if (scrollToModule != null)
                {
                    await scrollToModule.InvokeVoidAsync("scrollTo", "submit-button");
                }
            }
            
            ProductionLogEventService.MarkDirty();
        }

    }
    
    /// <summary>
    /// Determines if the operator has completed all steps in all production logs.
    /// </summary>
    /// <returns>Returns true if each step in every production log has been completed; false otherwise.</returns>
    private async Task<bool> GetWorkInstructionStatus()
    {
        try
        {
            return await Task.Run(() =>
            {
                // Check if every log's steps have attempts with valid submit times
                return ProductionLogBatch.Logs.All(log =>
                    log.LogSteps.All(step =>
                        step.Attempts.Any(a => a.SubmitTime != DateTimeOffset.MinValue)));
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking work instruction completion status");
            return false;
        }
    }
    
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        ProductionLogPartService.CurrentProductNumberChanged -= HandleProductNumberChanged;
        ProductionLogEventService.AutoSaveTriggered -= _autoSaveHandler;
        ProductionLogEventService.DbSaveTriggered -= SaveLogsToDatabaseHandler;
        await ProductionLogEventService.StopDbSaveTimerAsync();
        
        if (scrollToModule is not null)
        {
            try
            {
                await scrollToModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Deliberately not acting on the JSDisconnectedException since it is the preferred
                // way to handle disposed JS scripts without logging: https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-9.0
            }
            catch (JSException jsException)
            {
                Log.Warning("JS Interop Exception thrown, {Message}", jsException.Message);
            }
        }
    }
    
    private void AddProductionLogs(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var emptyLog = new ProductionLogFormDTO
            {
                LogSteps = ActiveWorkInstruction?.Nodes
                    .Where(n => n.NodeType == WorkInstructionNodeType.Step)
                    .Select(n => new LogStepFormDTO
                    {
                        WorkInstructionStepId = n.Id,
                        Attempts = [] // empty initially
                    })
                    .ToList() ?? []
            };

            ProductionLogBatch.Logs.Add(emptyLog);
            
            if (ActiveWorkInstruction == null) continue;
        }

        // Notify downstream services
        ProductionLogEventService.SetCurrentProductionLogs(ProductionLogBatch.Logs);
    }
}