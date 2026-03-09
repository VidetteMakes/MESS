using MESS.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace MESS.Data.Context;

/// <inheritdoc />
public class ApplicationContext : DbContext
{
    /// <inheritdoc />
    public ApplicationContext()
    {
        
    }
    /// <inheritdoc />
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
        
    }
    
    /// <summary>
    /// DbSet for WorkInstructions.
    /// </summary>
    public virtual DbSet<WorkInstruction> WorkInstructions { get; set; } = null!;
    /// <summary>
    /// DbSet for Steps.
    /// </summary>
    public virtual DbSet<Step> Steps { get; set; } = null!;
    /// <summary>
    /// DbSet for WorkInstructionNodes.
    /// </summary>
    public virtual DbSet<WorkInstructionNode> WorkInstructionNodes { get; set; } = null!;
    /// <summary>
    /// DbSet for PartNodes.
    /// </summary>
    public virtual DbSet<PartNode> PartNodes { get; set; } = null!;
    /// <summary>
    /// DbSet for ProductionLogParts.
    /// </summary>
    public virtual DbSet<ProductionLogPart> ProductionLogParts { get; set; } = null!;
    /// <summary>
    /// DbSet for ProductionLogs.
    /// </summary>
    public virtual DbSet<ProductionLog> ProductionLogs { get; set; } = null!;
    
    /// <summary>
    /// DbSet for ProductionLogSteps.
    /// </summary>
    public virtual DbSet<ProductionLogStep> ProductionLogSteps { get; set; } = null!;

    /// <summary>
    /// DbSet for ProductionLogStepAttempts.
    /// </summary>
    public virtual DbSet<ProductionLogStepAttempt> ProductionLogStepAttempts { get; set; } = null!;
    
    /// <summary>
    /// DbSet for Products.
    /// </summary>
    public virtual DbSet<Product> Products { get; set; } = null!;
    /// <summary>
    /// DbSet for Parts.
    /// </summary>
    public virtual DbSet<PartDefinition> PartDefinitions { get; set; } = null!;
    /// <summary>
    /// DbSet for SerializableParts.
    /// </summary>
    public virtual DbSet<SerializablePart> SerializableParts { get; set; } = null!;
    
    /// <summary>
    /// DbSet for Locations.
    /// </summary>
    public DbSet<Location> Locations { get; set; } = null!;
    
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Product>()
            .HasOne(p => p.PartDefinition)
            .WithOne() // One-to-one for now; can be changed to .WithMany() later.
            .HasForeignKey<Product>(p => p.PartDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkInstructionNode>()
            .UseTptMappingStrategy();

        modelBuilder.Entity<PartNode>()
            .ToTable("PartNodes")
            .HasOne(p => p.PartDefinition)
            .WithMany()
            .HasForeignKey(p => p.PartDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Step>()
            .ToTable("Steps");
        
        modelBuilder.Entity<ProductionLog>()
            .HasOne(p => p.WorkInstruction)
            .WithMany()
            .HasForeignKey("WorkInstructionId")
            .IsRequired(false);
        
        modelBuilder.Entity<ProductionLogPart>()
            .HasKey(plp => new { plp.ProductionLogId, plp.SerializablePartId, plp.OperationType });
        
        modelBuilder.Entity<ProductionLogPart>()
            .HasOne(plp => plp.SerializablePart)
            .WithMany(sp => sp.ProductionLogParts)
            .HasForeignKey(plp => plp.SerializablePartId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Location>()
            .HasIndex(l => l.Name)
            .IsUnique();
        
        modelBuilder.Entity<SerializablePart>()
            .HasOne(sp => sp.Location)
            .WithMany(l => l.SerializableParts)
            .HasForeignKey(sp => sp.LocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
    
    /// <inheritdoc />
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }
    
    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }
    
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // To be modified when User logic is added
                // entry.Entity.CreatedBy = "TheCreateUser";
                entry.Entity.CreatedOn = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                // To be modified when User logic is added
                // entry.Entity.LastModifiedBy = "TheUpdateUser";
                entry.Entity.LastModifiedOn = DateTime.UtcNow;
            }
        }
    }

}