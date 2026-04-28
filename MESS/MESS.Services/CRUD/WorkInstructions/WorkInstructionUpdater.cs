using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.CRUD.PartDefinitions;
using MESS.Services.CRUD.Products;
using MESS.Services.DTOs.WorkInstructions.Form;
using MESS.Services.DTOs.WorkInstructions.Nodes.Form;
using MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.Form;
using MESS.Services.DTOs.WorkInstructions.Nodes.StepNodes.Form;

namespace MESS.Services.CRUD.WorkInstructions;

/// <summary>
/// Provides logic for applying changes from a <see cref="WorkInstructionFormDTO"/>
/// to an existing <see cref="WorkInstruction"/> aggregate.
/// </summary>
/// <remarks>
/// This class is responsible for synchronizing scalar properties and
/// related collections such as nodes and products while respecting
/// Entity Framework Core tracking rules.  
/// 
/// It mutates tracked entities in place and does not persist changes;
/// callers are responsible for invoking <c>SaveChanges</c>.
/// </remarks>
public class WorkInstructionUpdater : IWorkInstructionUpdater
{
    private readonly IProductResolver _productResolver;
    private readonly IPartDefinitionResolver _partDefinitionResolver;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkInstructionUpdater"/> class.
    /// </summary>
    /// <param name="productResolver">The service used for resolving products from product names.</param>
    /// <param name="partDefinitionResolver">The service used for resolving the part definition that a work instruction produces.</param>
    public WorkInstructionUpdater(IProductResolver productResolver, IPartDefinitionResolver partDefinitionResolver)
    {
        _productResolver = productResolver;
        _partDefinitionResolver = partDefinitionResolver;
    }
    
    /// <summary>
    /// Applies the values from a <see cref="WorkInstructionFormDTO"/> to an existing
    /// tracked <see cref="WorkInstruction"/> entity.
    /// </summary>
    /// <remarks>
    /// This method mutates the provided <paramref name="entity"/> in place.
    /// The entity must already be loaded from the database with its related
    /// <see cref="WorkInstruction.Nodes"/> and <see cref="WorkInstruction.Products"/>
    /// collections included and tracked by the provided <paramref name="context"/>.
    /// <para>
    /// The method performs:
    /// <list type="bullet">
    /// <item>
    /// Updates of scalar properties (e.g., title, version, flags).
    /// </item>
    /// <item>
    /// Synchronization of the many-to-many <see cref="WorkInstruction.Products"/> collection.
    /// </item>
    /// <item>
    /// Synchronization of the <see cref="WorkInstruction.Nodes"/> collection,
    /// including creation, update, and removal of nodes.
    /// </item>
    /// </list>
    /// </para>
    /// This method does not call <c>SaveChanges</c>; the caller is responsible
    /// for persisting changes.
    /// </remarks>
    /// <param name="dto">
    /// The form DTO containing the desired state of the work instruction.
    /// </param>
    /// <param name="entity">
    /// The existing tracked work instruction entity to mutate.
    /// </param>
    /// <param name="context">
    /// The active <see cref="ApplicationContext"/> used for resolving
    /// related entities and tracking changes.
    /// </param>
    /// <returns>
    /// A task that completes when the mutation process has finished.
    /// </returns>
    public async Task ApplyAsync(
        WorkInstructionFormDTO dto,
        WorkInstruction entity,
        ApplicationContext context)
    {
        ApplyScalars(dto, entity);

        await SyncPartProducedAsync(dto, entity, context);
        await SyncProductsAsync(dto, entity, context);

        await SyncNodes(dto.Nodes, entity, context);
    }
    
    private void ApplyScalars(WorkInstructionFormDTO dto, WorkInstruction entity)
    {
        entity.Title = dto.Title;
        entity.Version = dto.Version;
        entity.IsActive = dto.IsActive;
        entity.ShouldGenerateQrCode = dto.ShouldGenerateQrCode;
        entity.PartProducedIsSerialized = dto.PartProducedIsSerialized;
    }

    private async Task SyncProductsAsync(
        WorkInstructionFormDTO dto,
        WorkInstruction entity,
        ApplicationContext context)
    {
        entity.Products = await _productResolver.ResolveProductsAsync(context, dto.ProductNames);
    }
    
    private async Task SyncNodes(
        List<WorkInstructionNodeFormDTO> formNodes,
        WorkInstruction entity,
        ApplicationContext context)
    {
        var existingById = entity.Nodes.ToDictionary(n => n.Id);

        var incomingIds = formNodes
            .Where(d => d.Id != 0)   // 0 means “new node”
            .Select(d => d.Id)
            .ToHashSet();

        // ---- Remove deleted ----
        var toRemove = entity.Nodes
            .Where(n => !incomingIds.Contains(n.Id))
            .ToList();

        foreach (var node in toRemove)
            context.Remove(node);

        // ---- Add / Update ----
        foreach (var dto in formNodes)
        {
            if (dto.Id != 0 && existingById.TryGetValue(dto.Id, out var existing))
            {
                await ApplyToExistingAsync(dto, existing, context);
            }
            else
            {
                var newNode = await CreateNewAsync(dto, context);
                entity.Nodes.Add(newNode);
            }
        }
    }
    
    /// <summary>
    /// Synchronizes the produced part of the work instruction with the value
    /// provided by the form DTO.
    /// </summary>
    /// <remarks>
    /// This method resolves the produced <see cref="PartDefinition"/> using
    /// the <see cref="IPartDefinitionResolver"/>. If the part does not exist,
    /// a new entity will be added to the current <see cref="ApplicationContext"/>
    /// and persisted when the caller saves change.
    /// </remarks>
    /// <param name="dto">
    /// The form DTO containing the desired produced part information.
    /// </param>
    /// <param name="entity">
    /// The tracked <see cref="WorkInstruction"/> entity being updated.
    /// </param>
    /// <param name="context">
    /// The active <see cref="ApplicationContext"/> used for resolving
    /// or creating the part definition.
    /// </param>
    private async Task SyncPartProducedAsync(
        WorkInstructionFormDTO dto,
        WorkInstruction entity,
        ApplicationContext context)
    {
        entity.PartProduced = await _partDefinitionResolver.ResolveAsync(context, dto.ProducedPartName, null);
    }
    
    private async Task ApplyToExistingAsync(
        WorkInstructionNodeFormDTO dto,
        WorkInstructionNode entity,
        ApplicationContext context)
    {
        entity.Position = dto.Position;

        switch (dto)
        {
            case StepNodeFormDTO stepDto when entity is Step step:
                ApplyStep(stepDto, step);
                break;

            case PartNodeFormDTO partDto when entity is PartNode part:
                await ApplyPartNodeAsync(partDto, part, context);
                break;

            default:
                throw new InvalidOperationException("Node type mismatch.");
        }
    }
    
    private async Task<WorkInstructionNode> CreateNewAsync(
        WorkInstructionNodeFormDTO dto,
        ApplicationContext context)
    {
        return dto switch
        {
            StepNodeFormDTO stepDto => new Step
            {
                NodeType = WorkInstructionNodeType.Step,
                Position = stepDto.Position,
                Name = stepDto.Name,
                Body = stepDto.Body,
                DetailedBody = stepDto.DetailedBody,
                PrimaryMedia = stepDto.PrimaryMedia.ToList(),
                SecondaryMedia = stepDto.SecondaryMedia.ToList()
            },

            PartNodeFormDTO partDto => await CreatePartNodeAsync(partDto, context),

            _ => throw new NotSupportedException()
        };
    }
    
    private void ApplyStep(StepNodeFormDTO dto, Step entity)
    {
        entity.Name = dto.Name;
        entity.Body = dto.Body;
        entity.DetailedBody = dto.DetailedBody;

        entity.PrimaryMedia = dto.PrimaryMedia.ToList();
        entity.SecondaryMedia = dto.SecondaryMedia.ToList();
    }
    
    private async Task ApplyPartNodeAsync(
        PartNodeFormDTO dto,
        PartNode entity,
        ApplicationContext context)
    {
        var part = await _partDefinitionResolver.ResolveAsync(context, dto.Name, dto.Number)
                   ?? throw new InvalidOperationException(
                       $"Failed to resolve PartDefinition '{dto.Name}' '{dto.Number}'.");

        entity.PartDefinitionId = part.Id;
        entity.PartDefinition = part;
    }
    
    private async Task<PartNode> CreatePartNodeAsync(
        PartNodeFormDTO dto,
        ApplicationContext context)
    {
        var part = await _partDefinitionResolver.ResolveAsync(context, dto.Name, dto.Number)
                   ?? throw new InvalidOperationException(
                       $"Failed to resolve PartDefinition '{dto.Name}' '{dto.Number}'.");

        return new PartNode
        {
            NodeType = WorkInstructionNodeType.Part,
            Position = dto.Position,
            PartDefinitionId = part.Id,
            PartDefinition = part
        };
    }
}