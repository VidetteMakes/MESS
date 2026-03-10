using MESS.Data.Context;
using MESS.Data.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MESS.Services.CRUD.ProductionLogParts;

/// <inheritdoc />
public class ProductionLogPartService : IProductionLogPartService
{
    private readonly IDbContextFactory<ApplicationContext> _contextFactory;
    /// <summary>
    /// Initializes a new instance of the <see cref="ProductionLogPartService"/> class.
    /// </summary>
    /// <param name="contextFactory">The application database context used for data operations.</param>
    public ProductionLogPartService(IDbContextFactory<ApplicationContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    /// <inheritdoc />
    public event Action? CurrentProductNumberChanged;
    private string? _currentProductNumber;

    /// <inheritdoc />
    public string? CurrentProductNumber
    {
        get => _currentProductNumber;
        set
        {
            _currentProductNumber = value;
            CurrentProductNumberChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public async Task<List<ProductionLogPart>?> GetAllAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var logList = await context.ProductionLogParts
                .Include(l => l.SerializablePart!)
                .ThenInclude(sp => sp.PartDefinition)
                .ToListAsync();

            return logList;
        }
        catch (Exception e)
        {
            Log.Warning("Exception caught while attempting to Get All ProductionLogParts Async: {ExceptionMessage}", e.Message);
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<List<int>> GetCurrentAssemblyPartIdsAsync(int rootSerializablePartId)
    {
        var result = new HashSet<int>();

        var rootLogId = await GetLatestProductionLogForPartAsync(rootSerializablePartId);

        if (rootLogId == null)
            return [];

        var installedParts = await GetCurrentInstalledPartIdsForLogAsync(rootLogId.Value);

        foreach (var partId in installedParts)
        {
            if (result.Add(partId))
            {
                var nestedParts = await GetCurrentAssemblyPartIdsAsync(partId);

                foreach (var nested in nestedParts)
                    result.Add(nested);
            }
        }

        return result.ToList();
    }
    
    /// <summary>
    /// Retrieves the most recent production log in which the specified part was produced.
    /// </summary>
    /// <param name="serializablePartId">The serialized part ID.</param>
    /// <returns>
    /// The production log ID if one exists; otherwise null.
    /// </returns>
    private async Task<int?> GetLatestProductionLogForPartAsync(int serializablePartId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.ProductionLogParts
            .Where(plp =>
                plp.SerializablePartId == serializablePartId &&
                plp.OperationType == PartOperationType.Produced)
            .OrderByDescending(plp => plp.ProductionLogId)
            .Select(plp => (int?)plp.ProductionLogId)
            .FirstOrDefaultAsync();
    }
    
    /// <summary>
    /// Retrieves the set of serialized part IDs currently installed in the assembly
    /// produced by the specified production log.
    /// </summary>
    /// <param name="productionLogId">The production log ID.</param>
    /// <returns>
    /// A list of serialized part IDs currently installed in that assembly.
    /// </returns>
    private async Task<List<int>> GetCurrentInstalledPartIdsForLogAsync(int productionLogId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var events = await context.ProductionLogParts
            .Where(plp => plp.ProductionLogId == productionLogId)
            .ToListAsync();

        var installed = events
            .Where(e => e.OperationType == PartOperationType.Installed)
            .Select(e => e.SerializablePartId)
            .ToHashSet();

        var removed = events
            .Where(e => e.OperationType == PartOperationType.Removed)
            .Select(e => e.SerializablePartId)
            .ToHashSet();

        installed.ExceptWith(removed);

        return installed.ToList();
    }

    /// <inheritdoc />
    public async Task<bool> CreateAsync(ProductionLogPart productionLogPart)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await context.ProductionLogParts.AddAsync(productionLogPart);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            Log.Warning("Exception caught while attempting to create ProductionLogPart Async: {ExceptionMessage}", e.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CreateRangeAsync(List<ProductionLogPart> productionLogParts)
    {
        if (productionLogParts.Count == 0)
        {
            Log.Warning("CreateRangeAsync called with an empty or null list of ProductionLogParts.");
            return false;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Remove potential duplicates based on composite key before inserting
            var distinctParts = productionLogParts
                .GroupBy(p => new { p.ProductionLogId, p.SerializablePartId, p.OperationType })
                .Select(g => g.First())
                .ToList();

            foreach (var logPart in distinctParts)
            {
                if (logPart.SerializablePart != null)
                {
                    // Attach existing SerializablePart without marking it as modified
                    context.Attach(logPart.SerializablePart);
                    context.Entry(logPart.SerializablePart).State = EntityState.Unchanged;
                }

                if (logPart.ProductionLog != null)
                {
                    // Same treatment for ProductionLog if passed in as a tracked reference
                    context.Attach(logPart.ProductionLog);
                    context.Entry(logPart.ProductionLog).State = EntityState.Unchanged;
                }
            }

            await context.ProductionLogParts.AddRangeAsync(distinctParts);
            var changes = await context.SaveChangesAsync();

            Log.Information("Successfully inserted {Count} ProductionLogPart records.", changes);
            return true;
        }
        catch (DbUpdateException dbEx)
        {
            Log.Error(dbEx, "Database error inserting ProductionLogPart range (constraint or duplicate key likely).");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error creating range of ProductionLogParts.");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(ProductionLogPart productionLogPart)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            context.ProductionLogParts.Update(productionLogPart);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            Log.Warning("Exception caught while attempting to update ProductionLogPart with Exception Message: {ExceptionMessage}", e.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int productionLogId, int serializablePartId, PartOperationType operationType)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.ProductionLogParts
                .FindAsync(productionLogId, serializablePartId, operationType);

            if (entity is null)
            {
                Log.Warning(
                    "Attempted to delete non-existent ProductionLogPart with keys: " +
                    "ProductionLogId={ProductionLogId}, SerializablePartId={SerializablePartId}, OperationType={OperationType}",
                    productionLogId, serializablePartId, operationType);
                return false;
            }

            context.ProductionLogParts.Remove(entity);
            await context.SaveChangesAsync();

            Log.Information(
                "Deleted ProductionLogPart successfully: ProductionLogId={ProductionLogId}, SerializablePartId={SerializablePartId}, OperationType={OperationType}",
                productionLogId, serializablePartId, operationType);

            return true;
        }
        catch (Exception e)
        {
            Log.Error(e,
                "Exception caught while attempting to delete ProductionLogPart with keys: " +
                "ProductionLogId={ProductionLogId}, SerializablePartId={SerializablePartId}, OperationType={OperationType}",
                productionLogId, serializablePartId, operationType);
            return false;
        }
    }
}