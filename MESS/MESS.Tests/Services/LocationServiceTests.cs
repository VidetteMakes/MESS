using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.CRUD.Locations;
using MESS.Services.CRUD.ProductionLogParts;
using MESS.Services.DTOs.Locations;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MESS.Tests.Services;

public class LocationServiceTests
{
    private readonly DbContextOptions<ApplicationContext> _options;
    private readonly Mock<IProductionLogPartService> _mockProductionLogPartService;
    private readonly LocationService _sut;

    public LocationServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _mockProductionLogPartService = new Mock<IProductionLogPartService>();

        var factory = new TestDbContextFactory(_options);

        _sut = new LocationService(factory, _mockProductionLogPartService.Object);
    }
    
    private class TestDbContextFactory(DbContextOptions<ApplicationContext> options)
        : IDbContextFactory<ApplicationContext>
    {
        public ApplicationContext CreateDbContext()
        {
            return new ApplicationContext(options);
        }

        public Task<ApplicationContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ApplicationContext(options));
        }
    }
    
    [Fact]
    public async Task CreateLocationAsync_ShouldCreateLocation()
    {
        var request = new CreateLocationRequest
        {
            Name = "Warehouse A"
        };

        // Act
        var result = await _sut.CreateLocationAsync(request);

        // Assert DTO
        Assert.NotNull(result);
        Assert.Equal("Warehouse A", result.Name);
        Assert.Equal(0, result.PartCount);
        Assert.True(result.Id > 0);

        // Assert database state
        await using var context = new ApplicationContext(_options);

        var location = await context.Locations.FirstAsync();

        Assert.Equal("Warehouse A", location.Name);
    }
    
    [Fact]
    public async Task CreateLocationAsync_ShouldAllowMultipleLocations()
    {
        var request1 = new CreateLocationRequest
        {
            Name = "Warehouse A"
        };

        var request2 = new CreateLocationRequest
        {
            Name = "Warehouse B"
        };

        await _sut.CreateLocationAsync(request1);
        await _sut.CreateLocationAsync(request2);

        await using var context = new ApplicationContext(_options);

        var locations = await context.Locations.ToListAsync();

        Assert.Equal(2, locations.Count);
        Assert.Contains(locations, l => l.Name == "Warehouse A");
        Assert.Contains(locations, l => l.Name == "Warehouse B");
    }
    
    [Fact]
    public async Task CreateLocationAsync_DuplicateName_ShouldThrow()
    {
        var request = new CreateLocationRequest { Name = "Warehouse A" };

        await _sut.CreateLocationAsync(request);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateLocationAsync(request));
    }
    
    [Fact]
    public async Task RenameLocationAsync_LocationDoesNotExist_ShouldThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var factory = new Mock<IDbContextFactory<ApplicationContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationContext(options));

        var mockPLPService = new Mock<IProductionLogPartService>();

        var service = new LocationService(factory.Object, mockPLPService.Object);

        var request = new RenameLocationRequest
        {
            LocationId = 999,
            NewName = "NewName"
        };

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RenameLocationAsync(request));
    }
    
    [Fact]
    public async Task RenameLocationAsync_DuplicateName_ShouldThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var context = new ApplicationContext(options))
        {
            context.Locations.AddRange(
                new Location { Id = 1, Name = "LocationA" },
                new Location { Id = 2, Name = "LocationB" }
            );

            await context.SaveChangesAsync();
        }

        var factory = new Mock<IDbContextFactory<ApplicationContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationContext(options));

        var mockPLPService = new Mock<IProductionLogPartService>();

        var service = new LocationService(factory.Object, mockPLPService.Object);

        var request = new RenameLocationRequest
        {
            LocationId = 1,
            NewName = "LocationB"
        };

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RenameLocationAsync(request));
    }

    [Fact]
    public async Task MoveSerializablePartAsync_ShouldMoveRootAndNestedParts()
    {
        await using var context = new ApplicationContext(_options);

        var locationA = new Location { Id = 1, Name = "LocationA" };
        var locationB = new Location { Id = 2, Name = "LocationB" };

        var root = new SerializablePart { Id = 1, SerialNumber = "Root", LocationId = 1 };
        var child1 = new SerializablePart { Id = 2, SerialNumber = "Child1", LocationId = 1 };
        var child2 = new SerializablePart { Id = 3, SerialNumber = "Child2", LocationId = 1 };

        context.Locations.AddRange(locationA, locationB);
        context.SerializableParts.AddRange(root, child1, child2);

        await context.SaveChangesAsync();

        _mockProductionLogPartService
            .Setup(x => x.GetCurrentAssemblyPartIdsAsync(1))
            .ReturnsAsync(new List<int> { 2, 3 });

        var request = new MoveSerializablePartRequest
        {
            SerializablePartId = 1,
            LocationId = 2
        };

        // Act
        await _sut.MoveSerializablePartAsync(request);

        await using var assertContext = new ApplicationContext(_options);

        var parts = await assertContext.SerializableParts.ToListAsync();

        Assert.All(parts, p => Assert.Equal(2, p.LocationId));
    }
    
    [Fact]
    public async Task MoveSerializablePartAsync_LocationDoesNotExist_ShouldThrow()
    {
        await using var context = new ApplicationContext(_options);

        var locationA = new Location { Id = 1, Name = "LocationA" };

        var root = new SerializablePart
        {
            Id = 1,
            SerialNumber = "Root",
            LocationId = 1
        };

        context.Locations.Add(locationA);
        context.SerializableParts.Add(root);

        await context.SaveChangesAsync();

        _mockProductionLogPartService
            .Setup(x => x.GetCurrentAssemblyPartIdsAsync(1))
            .ReturnsAsync(new List<int>());

        var request = new MoveSerializablePartRequest
        {
            SerializablePartId = 1,
            LocationId = 999 // location does not exist
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.MoveSerializablePartAsync(request));
    }
    
    [Fact]
    public async Task MoveSerializablePartAsync_NoNestedParts_ShouldMoveRootOnly()
    {
        await using var context = new ApplicationContext(_options);

        var locationA = new Location { Id = 1, Name = "LocationA" };
        var locationB = new Location { Id = 2, Name = "LocationB" };

        var root = new SerializablePart
        {
            Id = 1,
            SerialNumber = "Root",
            LocationId = 1
        };

        context.Locations.AddRange(locationA, locationB);
        context.SerializableParts.Add(root);

        await context.SaveChangesAsync();

        _mockProductionLogPartService
            .Setup(x => x.GetCurrentAssemblyPartIdsAsync(1))
            .ReturnsAsync(new List<int>()); // no nested parts

        var request = new MoveSerializablePartRequest
        {
            SerializablePartId = 1,
            LocationId = 2
        };

        // Act
        await _sut.MoveSerializablePartAsync(request);

        await using var assertContext = new ApplicationContext(_options);

        var part = await assertContext.SerializableParts.FirstAsync();

        Assert.Equal(2, part.LocationId);
    }
    
    [Fact]
    public async Task MoveSerializablePartAsync_ShouldNotMoveUnrelatedParts()
    {
        await using var context = new ApplicationContext(_options);

        var locationA = new Location { Id = 1, Name = "LocationA" };
        var locationB = new Location { Id = 2, Name = "LocationB" };

        var root = new SerializablePart
        {
            Id = 1,
            SerialNumber = "Root",
            LocationId = 1
        };

        var child = new SerializablePart
        {
            Id = 2,
            SerialNumber = "Child",
            LocationId = 1
        };

        var unrelated = new SerializablePart
        {
            Id = 3,
            SerialNumber = "Unrelated",
            LocationId = 1
        };

        context.Locations.AddRange(locationA, locationB);
        context.SerializableParts.AddRange(root, child, unrelated);

        await context.SaveChangesAsync();

        _mockProductionLogPartService
            .Setup(x => x.GetCurrentAssemblyPartIdsAsync(1))
            .ReturnsAsync(new List<int> { 2 });

        var request = new MoveSerializablePartRequest
        {
            SerializablePartId = 1,
            LocationId = 2
        };

        // Act
        await _sut.MoveSerializablePartAsync(request);

        await using var assertContext = new ApplicationContext(_options);

        var parts = await assertContext.SerializableParts.ToListAsync();

        var movedRoot = parts.First(p => p.Id == 1);
        var movedChild = parts.First(p => p.Id == 2);
        var untouched = parts.First(p => p.Id == 3);

        Assert.Equal(2, movedRoot.LocationId);
        Assert.Equal(2, movedChild.LocationId);
        Assert.Equal(1, untouched.LocationId); // should NOT move
    }
}