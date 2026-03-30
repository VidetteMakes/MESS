using MESS.Data.Context;
using MESS.Data.Models;
using MESS.Services.CRUD.PartDefinitions;

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

            var part = await _partDefinitionResolver.ResolveAsync(
                context,
                pending.Name,
                pending.Number,
                pending.IsSerialNumberUnique);

            if (part == null)
                throw new InvalidOperationException(
                    "PartNode has invalid part name.");

            node.PartDefinitionId = part.Id;
            node.PartDefinition = part;
        }
    }
}