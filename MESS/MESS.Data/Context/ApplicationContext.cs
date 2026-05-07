using MESS.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MESS.Data.Context;

/// <inheritdoc />
public class ApplicationContext
    : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
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
    /// DbSet for SerializablePartRelationships
    /// </summary>
    public virtual DbSet<SerializablePartRelationship> SerializablePartRelationships { get; set; } = null!;
    
    /// <summary>
    /// DbSet for Tags.
    /// </summary>
    public virtual DbSet<Tag> Tags { get; set; } = null!;
    
    /// <summary>
    /// DbSet for TagHistories.
    /// </summary>
    public virtual DbSet<TagHistory> TagHistories { get; set; } = null!;
    
    /// <summary>
    /// DbSet for the Git Repository Configuration
    /// </summary>
    public virtual DbSet<GitConfiguration> GitConfiguration => Set<GitConfiguration>();
    
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Product>()
            .HasOne(p => p.PartDefinition)
            .WithOne() // One-to-one for now; can be changed to .WithMany() later.
            .HasForeignKey<Product>(p => p.PartDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkInstruction>()
            .HasIndex(w => new { w.Title, w.Version })
            .IsUnique();
        
        modelBuilder.Entity<WorkInstructionNode>()
            .UseTptMappingStrategy();

        modelBuilder.Entity<PartNode>()
            .ToTable("PartNodes")
            .HasOne(p => p.PartDefinition)
            .WithMany()
            .HasForeignKey(p => p.PartDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<PartDefinition>()
            .HasIndex(p => new { p.Name, p.Number })
            .IsUnique();

        modelBuilder.Entity<PartDefinition>()
            .HasIndex(p => p.Name)
            .IsUnique()
            .HasFilter("\"Number\" IS NULL OR \"Number\" = ''");
        
        modelBuilder.Entity<PartDefinition>()
            .Property(p => p.IsSerialNumberUnique)
            .HasDefaultValue(true);

        modelBuilder.Entity<PartDefinition>()
            .Property(p => p.InputType)
            .HasDefaultValue(PartInputType.SerialNumber);
        
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
        
        // Tag -> TagHistory: Cascade delete when a tag is deleted
        // EF Core would infer the FK and navigation automatically
        modelBuilder.Entity<Tag>()
            .HasMany(t => t.History)
            .WithOne(h => h.Tag)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Tag -> SerializablePart: Optional relationship
        // Default EF behavior would be "Restrict", we want SetNull on delete
        modelBuilder.Entity<Tag>()
            .HasOne(t => t.SerializablePart)
            .WithMany() // no collection on SerializablePart
            .OnDelete(DeleteBehavior.SetNull);
        
        // TagHistory -> SerializablePart: Optional relationship
        // Default EF behavior would be "Restrict", we want SetNull on delete
        modelBuilder.Entity<TagHistory>()
            .HasOne<SerializablePart>()
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);
        
        // Index on Tag.Code for faster lookup and uniqueness
        // EF Core does NOT create this automatically
        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Code)
            .IsUnique();
        
        modelBuilder.Entity<SerializablePartRelationship>(entity =>
        {
            // Child relationship: required one-to-one
            entity.HasOne(r => r.ChildPart)
                .WithOne(p => p.ParentRelationship)
                .HasForeignKey<SerializablePartRelationship>(r => r.ChildPartId);

            // Parent relationship: one-to-many
            entity.HasOne(r => r.ParentPart)
                .WithMany(p => p.ChildrenRelationships);
            
            // Ensure a child can only have one parent
            entity.HasIndex(r => r.ChildPartId).IsUnique();
        });
        
        modelBuilder.Entity<FailureNoun>()
            .HasMany(fn => fn.Adjectives)
            .WithMany(fa => fa.Nouns)
            .UsingEntity(j => j.ToTable("FailureNounAdjectives"));

        modelBuilder.Entity<GitConfiguration>(entity =>
        {
            // enforce single row pattern
            entity.Property(x => x.Id)
                .ValueGeneratedNever();

            entity.Property(x => x.RemoteUrl)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(x => x.Branch)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.CredentialReference)
                .HasMaxLength(500);

            entity.Property(x => x.UpdatedAtUtc)
                .IsRequired();

            // Store enum as string (recommended)
            entity.Property(x => x.AuthType)
                .HasConversion<string>()
                .HasMaxLength(50);
        });
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
            switch (entry.State)
            {
                case EntityState.Added:
                    // To be modified when User logic is added
                    // entry.Entity.CreatedBy = "TheCreateUser";
                    entry.Entity.CreatedOn = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    // To be modified when User logic is added
                    // entry.Entity.LastModifiedBy = "TheUpdateUser";
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

}