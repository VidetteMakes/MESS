using MESS.Data.Context;
using MESS.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MESS.Services.CRUD.OperatorTraining;

/// <inheritdoc />
public class OperatorTrainingService : IOperatorTrainingService
{
    private readonly IDbContextFactory<ApplicationContext> _contextFactory;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperatorTrainingService"/> class.
    /// </summary>
    public OperatorTrainingService(
        IDbContextFactory<ApplicationContext> contextFactory,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _contextFactory = contextFactory;
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OperatorTrainingModule>> GetAllAsync(bool includeDrafts = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.OperatorTrainingModules.AsNoTracking();

        if (!includeDrafts)
        {
            query = query.Where(module => module.IsPublished);
        }

        return await query
            .OrderBy(module => module.DisplayOrder)
            .ThenBy(module => module.Title)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<OperatorTrainingModule> CreateAsync(OperatorTrainingModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var currentUser = await GetCurrentUserNameAsync();
        module.Title = module.Title.Trim();
        module.Summary = module.Summary.Trim();
        module.Content = module.Content.Trim();
        module.CreatedBy = currentUser;
        module.LastModifiedBy = currentUser;

        context.OperatorTrainingModules.Add(module);
        await context.SaveChangesAsync();

        Log.Information("Created operator training module {TrainingModuleId}", module.Id);

        return module;
    }

    /// <inheritdoc />
    public async Task<OperatorTrainingModule?> UpdateAsync(OperatorTrainingModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var currentUser = await GetCurrentUserNameAsync();

        var existingModule = await context.OperatorTrainingModules
            .FirstOrDefaultAsync(existing => existing.Id == module.Id);

        if (existingModule is null)
        {
            Log.Warning("Operator training module {TrainingModuleId} was not found for update", module.Id);
            return null;
        }

        existingModule.Title = module.Title.Trim();
        existingModule.Summary = module.Summary.Trim();
        existingModule.Content = module.Content.Trim();
        existingModule.DisplayOrder = module.DisplayOrder;
        existingModule.IsPublished = module.IsPublished;
        existingModule.LastModifiedBy = currentUser;

        await context.SaveChangesAsync();

        Log.Information("Updated operator training module {TrainingModuleId}", module.Id);

        return existingModule;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var module = await context.OperatorTrainingModules.FindAsync(id);

        if (module is null)
        {
            Log.Warning("Operator training module {TrainingModuleId} was not found for deletion", id);
            return;
        }

        context.OperatorTrainingModules.Remove(module);
        await context.SaveChangesAsync();

        Log.Information("Deleted operator training module {TrainingModuleId}", id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrainingStep>> GetStepsAsync(IEnumerable<int>? moduleIds = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.TrainingSteps.AsNoTracking();

        if (moduleIds is not null)
        {
            var ids = moduleIds.Distinct().ToList();
            if (ids.Count > 0)
            {
                query = query.Where(step => ids.Contains(step.OperatorTrainingModuleId));
            }
        }

        return await query
            .OrderBy(step => step.OperatorTrainingModuleId)
            .ThenBy(step => step.StepOrder)
            .ThenBy(step => step.Title)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TrainingStep> CreateStepAsync(TrainingStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var currentUser = await GetCurrentUserNameAsync();
        step.Title = step.Title.Trim();
        step.Description = step.Description.Trim();
        step.CreatedBy = currentUser;
        step.LastModifiedBy = currentUser;

        context.TrainingSteps.Add(step);
        await context.SaveChangesAsync();

        Log.Information("Created training step {TrainingStepId}", step.Id);

        return step;
    }

    /// <inheritdoc />
    public async Task<TrainingStep?> UpdateStepAsync(TrainingStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var currentUser = await GetCurrentUserNameAsync();

        var existingStep = await context.TrainingSteps.FirstOrDefaultAsync(existing => existing.Id == step.Id);
        if (existingStep is null)
        {
            Log.Warning("Training step {TrainingStepId} was not found for update", step.Id);
            return null;
        }

        existingStep.Title = step.Title.Trim();
        existingStep.Description = step.Description.Trim();
        existingStep.StepOrder = step.StepOrder;
        existingStep.ImagePath = step.ImagePath;
        existingStep.OperatorTrainingModuleId = step.OperatorTrainingModuleId;
        existingStep.LastModifiedBy = currentUser;

        await context.SaveChangesAsync();

        Log.Information("Updated training step {TrainingStepId}", step.Id);

        return existingStep;
    }

    /// <inheritdoc />
    public async Task DeleteStepAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var step = await context.TrainingSteps.FindAsync(id);
        if (step is null)
        {
            Log.Warning("Training step {TrainingStepId} was not found for deletion", id);
            return;
        }

        context.TrainingSteps.Remove(step);
        await context.SaveChangesAsync();

        Log.Information("Deleted training step {TrainingStepId}", id);
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
