using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.CRUD.PartDefinitions;
using Microsoft.EntityFrameworkCore;

namespace MESS.Services.CRUD.WorkInstructions;

/// <inheritdoc/>
public class PartNodeResolver : IPartNodeResolver
{
    private readonly IPartDefinitionResolver _partDefinitionResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartNodeResolver"/> class.
    /// </summary>
    /// <param name="partDefinitionResolver"> The service for resolving part definitions </param>
    /// <remarks>
    /// This resolver uses the context to look up existing <see cref="PartDefinition"/>s
    /// and create new ones as needed when resolving <see cref="PartNode"/>s
    /// prior to saving a work instruction.
    /// </remarks>
    public PartNodeResolver(IPartDefinitionResolver partDefinitionResolver)
    {
        _partDefinitionResolver = partDefinitionResolver;
    }

    /// <inheritdoc/>
    public async Task ResolvePendingNodesAsync(
        ApplicationContext context,
        IEnumerable<WorkInstructionNode> nodes)
    {
        var partNodes = nodes
            .OfType<PartNode>()
            .Where(n => n.PartDefinition != null)
            .ToList();

        if (partNodes.Count == 0)
            return;

        foreach (var node in partNodes)
        {
            var pending = node.PartDefinition
                          ?? throw new InvalidOperationException(
                              "PartNode has no pending PartDefinition.");

            var inputType = pending.InputType;
            var isSerialUnique = pending.IsSerialNumberUnique;

            var part = await _partDefinitionResolver.ResolveAsync(
                context,
                pending.Name,
                pending.Number);

            if (part == null)
                throw new InvalidOperationException(
                    "PartNode has invalid part name.");

            // When a brand-new PartDefinition row was added, apply traceability fields
            // from the WI/import payload. Existing rows are owned by Part Definitions.
            if (context.Entry(part).State == EntityState.Added)
            {
                part.InputType = inputType;
                part.IsSerialNumberUnique = isSerialUnique;
            }

            node.PartDefinitionId = part.Id;
            node.PartDefinition = part;
        }
    }
}
