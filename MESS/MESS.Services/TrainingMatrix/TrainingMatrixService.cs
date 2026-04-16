#pragma warning disable CS1591
using MESS.Data.Context;
using MESS.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace MESS.Services.TrainingMatrix;

public sealed class TrainingMatrixService : ITrainingMatrixService
{
    private readonly IDbContextFactory<ApplicationContext> _contextFactory;
    private readonly ITrainingMatrixStatusStore _statusStore;

    public TrainingMatrixService(
        IDbContextFactory<ApplicationContext> contextFactory,
        ITrainingMatrixStatusStore statusStore)
    {
        _contextFactory = contextFactory;
        _statusStore = statusStore;
    }

    public async Task<TrainingMatrixSnapshot> GetSnapshotAsync(bool includeDrafts, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var modulesQuery = context.OperatorTrainingModules
            .AsNoTracking()
            .OrderBy(module => module.DisplayOrder)
            .ThenBy(module => module.Title);

        var modules = includeDrafts
            ? await modulesQuery.ToListAsync(cancellationToken)
            : await modulesQuery.Where(module => module.IsPublished).ToListAsync(cancellationToken);

        var moduleIds = modules.Select(module => module.Id).ToList();

        var steps = await context.TrainingSteps
            .AsNoTracking()
            .Where(step => moduleIds.Contains(step.OperatorTrainingModuleId))
            .OrderBy(step => step.OperatorTrainingModuleId)
            .ThenBy(step => step.StepOrder)
            .ThenBy(step => step.Title)
            .ToListAsync(cancellationToken);

        var users = await context.Users
            .AsNoTracking()
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.UserName)
            .ToListAsync(cancellationToken);

        var operators = users.Select(user => new TrainingMatrixOperator
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            DisplayName = ResolveDisplayName(user),
            IsActive = user.IsActive
        }).ToList();

        var moduleById = modules.ToDictionary(module => module.Id);
        var stepsByKey = steps.ToDictionary(
            step => BuildStepKey(step.OperatorTrainingModuleId, step.Title, step.Id),
            step => step);

        var records = _statusStore.GetRecords();
        var cellsByUserAndStep = new Dictionary<string, TrainingMatrixCell>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var step = ResolveStep(record, stepsByKey, steps);
            if (step is null)
            {
                continue;
            }

            var userKey = !string.IsNullOrWhiteSpace(record.UserId) ? record.UserId : record.UserName;
            if (string.IsNullOrWhiteSpace(userKey))
            {
                continue;
            }

            cellsByUserAndStep[BuildCellKey(userKey, step.Id)] = new TrainingMatrixCell
            {
                Score = Math.Clamp(record.Score, 0, 5),
                LastUpdatedOnUtc = record.LastUpdatedOnUtc,
                Source = record.Source
            };
        }

        var competencies = steps.Select(step =>
        {
            var module = moduleById[step.OperatorTrainingModuleId];
            var rowCells = new Dictionary<string, TrainingMatrixCell>(StringComparer.OrdinalIgnoreCase);
            var scoredValues = new List<int>();

            foreach (var user in operators)
            {
                var cell = ResolveCell(user, step.Id, cellsByUserAndStep);
                rowCells[user.UserId] = cell;
                if (cell.Score > 0)
                {
                    scoredValues.Add(cell.Score);
                }
            }

            return new TrainingMatrixCompetencyRow
            {
                StepId = step.Id,
                ModuleId = step.OperatorTrainingModuleId,
                ModuleTitle = module.Title,
                Title = step.Title,
                Description = step.Description,
                StepOrder = step.StepOrder,
                Coverage = scoredValues.Count == 0 ? 0 : Math.Round(scoredValues.Average(), 1),
                Cells = rowCells
            };
        }).ToList();

        return new TrainingMatrixSnapshot
        {
            Modules = modules,
            Operators = operators,
            Competencies = competencies,
            MqttState = _statusStore.GetMqttState()
        };
    }

    private static TrainingStep? ResolveStep(
        TrainingMatrixScoreRecord record,
        IReadOnlyDictionary<string, TrainingStep> stepsByKey,
        IReadOnlyCollection<TrainingStep> steps)
    {
        if (record.StepId is int stepId)
        {
            return steps.FirstOrDefault(step => step.Id == stepId);
        }

        if (record.ModuleId is not int moduleId || string.IsNullOrWhiteSpace(record.StepTitle))
        {
            return null;
        }

        return stepsByKey.TryGetValue(BuildStepKey(moduleId, record.StepTitle, null), out var step) ? step : null;
    }

    private static TrainingMatrixCell ResolveCell(
        TrainingMatrixOperator user,
        int stepId,
        IReadOnlyDictionary<string, TrainingMatrixCell> cellsByUserAndStep)
    {
        if (cellsByUserAndStep.TryGetValue(BuildCellKey(user.UserId, stepId), out var byId))
        {
            return byId;
        }

        if (!string.IsNullOrWhiteSpace(user.UserName) &&
            cellsByUserAndStep.TryGetValue(BuildCellKey(user.UserName, stepId), out var byUserName))
        {
            return byUserName;
        }

        return new TrainingMatrixCell { Score = 0 };
    }

    private static string ResolveDisplayName(ApplicationUser user)
    {
        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? user.UserName ?? user.Email ?? user.Id : displayName;
    }

    private static string BuildStepKey(int moduleId, string? stepTitle, int? stepId)
    {
        return $"{moduleId}::{stepId?.ToString() ?? string.Empty}::{stepTitle?.Trim() ?? string.Empty}";
    }

    private static string BuildCellKey(string userKey, int stepId)
    {
        return $"{userKey.Trim()}::{stepId}";
    }
}
#pragma warning restore CS1591
