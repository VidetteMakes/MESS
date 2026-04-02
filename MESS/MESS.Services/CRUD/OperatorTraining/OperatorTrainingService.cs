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
