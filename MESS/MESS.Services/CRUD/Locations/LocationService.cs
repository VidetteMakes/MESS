using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.CRUD.ProductionLogParts;
using MESS.Services.DTOs.Locations;
using Microsoft.EntityFrameworkCore;

namespace MESS.Services.CRUD.Locations;

/// <inheritdoc />
public class LocationService : ILocationService
{
    private readonly IDbContextFactory<ApplicationContext> _contextFactory;
    
    private readonly IProductionLogPartService _productionLogPartService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationService"/> class.
    /// </summary>
    /// <param name="contextFactory">The application database context used for data operations.</param>
    /// <param name="productionLogPartService">The service used to get data/objects concerning production log parts</param>
    public LocationService(IDbContextFactory<ApplicationContext> contextFactory, IProductionLogPartService productionLogPartService)
    {
        _contextFactory = contextFactory;
        _productionLogPartService = productionLogPartService;
    }
    
    /// <inheritdoc />
    public async Task<LocationDTO> CreateLocationAsync(CreateLocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Guard against duplicate names
        var exists = await context.Locations
            .AnyAsync(l => l.Name == request.Name);

        if (exists)
            throw new InvalidOperationException(
                $"A location named '{request.Name}' already exists.");

        var location = new Location
        {
            Name = request.Name
        };

        context.Locations.Add(location);

        await context.SaveChangesAsync();

        return new LocationDTO
        {
            Id = location.Id,
            Name = location.Name,
            PartCount = 0
        };
    }

    /// <inheritdoc />
    public async Task RenameLocationAsync(RenameLocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var duplicate = await context.Locations
            .AnyAsync(l => l.Name == request.NewName && l.Id != request.LocationId);

        if (duplicate)
            throw new InvalidOperationException(
                $"A location named '{request.NewName}' already exists.");

        var rows = await context.Locations
            .Where(l => l.Id == request.LocationId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(l => l.Name, request.NewName));

        if (rows == 0)
            throw new InvalidOperationException(
                $"Location {request.LocationId} does not exist.");
    }

    /// <inheritdoc />
    public async Task DeleteLocationAsync(DeleteLocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var location = await context.Locations
            .FirstOrDefaultAsync(l => l.Id == request.LocationId);

        if (location == null)
            throw new InvalidOperationException($"Location {request.LocationId} does not exist.");

        var hasParts = await context.SerializableParts
            .AnyAsync(p => p.LocationId == request.LocationId);

        if (hasParts)
            throw new InvalidOperationException(
                $"Location '{location.Name}' cannot be deleted because parts are still assigned to it.");

        context.Locations.Remove(location);

        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<LocationDTO?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Locations
            .Where(l => l.Id == id)
            .Select(l => new LocationDTO
            {
                Id = l.Id,
                Name = l.Name,
                PartCount = l.SerializableParts.Count()
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<LocationDTO?> GetByNameAsync(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Locations
            .Where(l => l.Name == name)
            .Select(l => new LocationDTO
            {
                Id = l.Id,
                Name = l.Name,
                PartCount = l.SerializableParts.Count()
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task MoveSerializablePartAsync(MoveSerializablePartRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Ensure destination location exists
        var location = await context.Locations.FirstOrDefaultAsync(l => l.Id == request.LocationId);
        if (location == null)
            throw new InvalidOperationException($"Location {request.LocationId} does not exist.");

        // Get all part IDs in the assembly
        var nestedPartIds = await _productionLogPartService.GetCurrentAssemblyPartIdsAsync(request.SerializablePartId);
        var allPartIds = nestedPartIds.Append(request.SerializablePartId).ToList();

        // Load all parts in the same context to satisfy FK constraints
        var partsToMove = await context.SerializableParts
            .Where(p => allPartIds.Contains(p.Id))
            .ToListAsync();

        // Update using navigation property
        foreach (var part in partsToMove)
        {
            part.Location = location;  // EF Core sets LocationId under the hood
        }

        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<LocationDTO>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Locations
            .Select(l => new LocationDTO
            {
                Id = l.Id,
                Name = l.Name,
                PartCount = l.SerializableParts.Count()
            })
            .OrderBy(l => l.Name)
            .ToListAsync();
    }
}