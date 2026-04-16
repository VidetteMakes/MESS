using MESS.Data.Context;
using MESS.Services.CRUD.ProductionLogs;
using MESS.Services.DTOs.ProductionLogs.Form;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using MESS.Data.Models;
using MESS.Services.CRUD.SerializableParts;

namespace MESS.Tests.Services;

public class ProductionLogServiceTests : IDisposable
{
    private static readonly SqliteConnection Connection = new SqliteConnection("DataSource=:memory:");
    private readonly DbContextOptions<ApplicationContext> _options;

    static ProductionLogServiceTests()
    {
        Connection.Open();
    }

    public ProductionLogServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(Connection)
            .Options;

        // Ensure clean database for each test
        using var context = new ApplicationContext(_options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");
    }

    private ProductionLogService CreateService()
    {
        var dbFactory = new Mock<IDbContextFactory<ApplicationContext>>();
        var mockSerializablePartService = new Mock<ISerializablePartService>();
        
        dbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new ApplicationContext(_options);
                ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");
                return ctx;
            });

        return new ProductionLogService(dbFactory.Object, mockSerializablePartService.Object);
    }

    private static async Task<(int productId, int workInstructionId)> SeedProductAndWIAsync(string wiTitle, DbContextOptions<ApplicationContext> options)
    {
        await using var context = new ApplicationContext(options);
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");

        var product = new Product { PartDefinition = new PartDefinition { Name = "Test Product", Number = "TEST-1" } };
        var wi = new WorkInstruction { Title = wiTitle };

        context.Products.Add(product);
        context.WorkInstructions.Add(wi);
        await context.SaveChangesAsync();

        return (product.Id, wi.Id);
    }

    [Fact]
    public async Task SaveOrUpdateBatchAsync_EmptyLog_CreatesLog()
    {
        var service = CreateService();
        var (productId, workInstructionId) = await SeedProductAndWIAsync("Test WI", _options);

        var formDtos = new List<ProductionLogFormDTO> { new() };
        var result = await service.SaveOrUpdateBatchAsync(formDtos, "tester", "op-1", productId, workInstructionId);

        Assert.Equal(1, result.CreatedCount);
        Assert.Single(result.CreatedIds);

        var log = await service.GetByIdAsync(result.CreatedIds.First());
        Assert.NotNull(log);
        Assert.Equal(productId, log!.ProductId);
        Assert.Equal(workInstructionId, log.WorkInstructionId);
    }

    [Fact]
    public async Task SaveOrUpdateBatchAsync_WithWorkInstruction_AssignsCorrectWI()
    {
        var service = CreateService();
        var (productId, workInstructionId) = await SeedProductAndWIAsync("WI-100", _options);

        var formDtos = new List<ProductionLogFormDTO> { new() };
        var result = await service.SaveOrUpdateBatchAsync(formDtos, "tester", "op-2", productId, workInstructionId);

        Assert.Equal(1, result.CreatedCount);
        var log = await service.GetByIdAsync(result.CreatedIds.First());
        Assert.NotNull(log);
        Assert.Equal(workInstructionId, log!.WorkInstructionId);
    }

    [Fact]
    public async Task SaveOrUpdateBatchAsync_UpdatesExistingLog()
    {
        var service = CreateService();
        var (productId, workInstructionId) = await SeedProductAndWIAsync("Update Test WI", _options);

        // Create
        var createForm = new ProductionLogFormDTO();
        var createResult = await service.SaveOrUpdateBatchAsync(new[] { createForm }, "tester", "op-4", productId, workInstructionId);
        var createdId = createResult.CreatedIds.First();

        // Update
        var updateForm = new ProductionLogFormDTO { Id = createdId };
        var updateResult = await service.SaveOrUpdateBatchAsync(new[] { updateForm }, "tester", "op-4", productId, workInstructionId);

        Assert.Equal(1, updateResult.UpdatedCount);
        var log = await service.GetByIdAsync(createdId);
        Assert.NotNull(log);
        Assert.Equal(productId, log!.ProductId);
        Assert.Equal(workInstructionId, log.WorkInstructionId);
    }

    [Fact]
    public async Task SaveOrUpdateBatchAsync_MultipleLogs_CreatesAllSuccessfully()
    {
        var service = CreateService();
        var (productId, workInstructionId) = await SeedProductAndWIAsync("Batch WI", _options);

        var batch = new List<ProductionLogFormDTO>
        {
            new(),
            new(),
            new()
        };

        var result = await service.SaveOrUpdateBatchAsync(batch, "tester", "op-1", productId, workInstructionId);

        Assert.Equal(3, result.CreatedCount);
        Assert.Equal(3, result.CreatedIds.Count);

        await using var context = new ApplicationContext(_options);
        var logs = await context.ProductionLogs
            .Where(l => result.CreatedIds.Contains(l.Id))
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, logs.Count);
        Assert.All(logs, l => Assert.Equal(workInstructionId, l.WorkInstructionId));
    }

    public void Dispose()
    {
        // Keep the in-memory connection open for all tests
    }
}
