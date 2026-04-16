using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.CRUD.PartDefinitions;
using MESS.Services.CRUD.ProductionLogs;
using MESS.Services.CRUD.Products;
using MESS.Services.CRUD.WorkInstructions;
using MESS.Services.Media.WorkInstructions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace MESS.Tests.Services;

public class WorkInstructionServiceTests
{
    private const string CONNECTION_STRING = "Data Source=WorkInstructionTestDatabase.db";
    private static WorkInstructionService MockWorkInstructionService ()
    {
        // Configure DbContextOptions to use SQLite with a file-based database
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(CONNECTION_STRING)
            .Options;

        // Ensure the database is created before tests run
        using (var context = new ApplicationContext(options))
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        // Set up the DbContextFactory to return a properly configured ApplicationContext instance
        var dbFactory = new Mock<IDbContextFactory<ApplicationContext>>();
        dbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationContext(options)); // Create new instance on each call

        dbFactory.Setup(f => f.CreateDbContext())
            .Returns(() => new ApplicationContext(options)); // Create new instance on each call
        
        var logMock = new Mock<IProductionLogService>();
        var imageMock = new Mock<IWorkInstructionImageService>();
        var memoryCacheMock = new Mock<IMemoryCache>();
        var updaterMock = new Mock<IWorkInstructionUpdater>();
        var nodeResolverMock = new Mock<IPartNodeResolver>();
        var partResolverMock = new Mock<IPartDefinitionResolver>();
        var productResolverMock = new Mock<IProductResolver>();
        
        return new WorkInstructionService(logMock.Object, imageMock.Object, memoryCacheMock.Object, updaterMock.Object, 
            productResolverMock.Object, nodeResolverMock.Object, partResolverMock.Object, dbFactory.Object);
    }
    
    [Fact]
    public void GetByTitle_DbContextException_ReturnsNull()
    {
        // Arrange
        var dbFactory = new Mock<IDbContextFactory<ApplicationContext>>();
        dbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));
    
        var logMock = new Mock<IProductionLogService>();
        var imageMock = new Mock<IWorkInstructionImageService>();
        var memoryCacheMock = new Mock<IMemoryCache>();
        var updaterMock = new Mock<IWorkInstructionUpdater>();
        var nodeResolverMock = new Mock<IPartNodeResolver>();
        var partResolverMock = new Mock<IPartDefinitionResolver>();
        var productResolverMock = new Mock<IProductResolver>();
    
        var service = new WorkInstructionService(logMock.Object, imageMock.Object, memoryCacheMock.Object, updaterMock.Object, 
            productResolverMock.Object, nodeResolverMock.Object, partResolverMock.Object, dbFactory.Object);

        // Act
        var result = service.GetByTitle("Test Title");

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetByTitle_CaseInsensitive_ReturnsWorkInstruction()
    {
        // Arrange
        var service = MockWorkInstructionService();
        var title = "Case Sensitive Test";
        var workInstruction = new WorkInstruction
        {
            Title = title,
            Nodes = [new Step { Name = "Test Step", Body = "Test Step" }]
        };

        // Add the work instruction to the test database
        await using var context = new ApplicationContext(
            new DbContextOptionsBuilder<ApplicationContext>()
                .UseSqlite(CONNECTION_STRING)
                .Options);
        context.WorkInstructions.Add(workInstruction);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = service.GetByTitle("CASE SENSITIVE TEST");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(title, result.Title);
    }
    
    [Fact]
    public void GetByTitle_EmptyTitle_ReturnsNull()
    {
        // Arrange
        var service = MockWorkInstructionService();
    
        // Act
        var result = service.GetByTitle("");
    
        // Assert
        Assert.Null(result);
    }
}