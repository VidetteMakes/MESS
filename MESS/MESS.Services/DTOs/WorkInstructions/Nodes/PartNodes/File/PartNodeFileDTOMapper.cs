using MESS.Data.Models;
using MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.Form;

namespace MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.File;

/// <summary>
/// Provides mapping extensions between <see cref="PartNode"/> and <see cref="PartNodeFileDTO"/>.
/// </summary>
public static class PartNodeFileDTOMapper
{
    /// <summary>
    /// Converts a <see cref="PartNode"/> entity to a <see cref="PartNodeFileDTO"/>.
    /// </summary>
    public static PartNodeFileDTO ToFileDTO(this PartNode entity)
    {
        if (entity.PartDefinition is null)
            throw new InvalidOperationException(
                $"PartDefinition not loaded for PartNode {entity.Id}");

        return new PartNodeFileDTO
        {
            Position = entity.Position,
            PartName = entity.PartDefinition.Name,
            PartNumber = entity.PartDefinition.Number,
            IsSerialNumberUnique = entity.PartDefinition.IsSerialNumberUnique,
            InputType = entity.PartDefinition.InputType
        };
    }

    /// <summary>
    /// Converts a <see cref="PartNodeFileDTO"/> to a <see cref="PartNode"/> entity.
    /// 
    /// Requires a resolved <see cref="PartDefinition"/> instance.
    /// </summary>
    public static PartNode ToEntity(
        this PartNodeFileDTO dto,
        PartDefinition resolvedPart)
    {
        return new PartNode
        {
            Position = dto.Position,
            NodeType = WorkInstructionNodeType.Part,
            PartDefinition = resolvedPart,
            PartDefinitionId = resolvedPart.Id,
        };
    }
    
    /// <summary>
    /// Converts a <see cref="PartNodeFileDTO"/> into a <see cref="PartNodeFormDTO"/>
    /// suitable for editing in the UI.
    /// </summary>
    /// <param name="dto">
    /// The file DTO representing a part node, typically imported from a work instruction file.
    /// </param>
    /// <returns>
    /// A new <see cref="PartNodeFormDTO"/> containing the part information from the file,
    /// including position, node type, part name, part number, and input type.
    /// </returns>
    /// <remarks>
    /// This method performs a direct mapping from the file DTO to the form DTO without
    /// resolving any database entities. The referenced part may or may not already exist
    /// in the system. Resolution or creation of the corresponding <see cref="PartDefinition"/>
    /// occurs later during the work instruction save process.
    /// </remarks>
    public static PartNodeFormDTO ToFormDTO(this PartNodeFileDTO dto)
    {
        return new PartNodeFormDTO
        {
            Position = dto.Position,
            NodeType = dto.NodeType,
            Name = dto.PartName,
            Number = dto.PartNumber,
            IsSerialNumberUnique = dto.IsSerialNumberUnique,
            InputType = dto.InputType
        };
    }
}
