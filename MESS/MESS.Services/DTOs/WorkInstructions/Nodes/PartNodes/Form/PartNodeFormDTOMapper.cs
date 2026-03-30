using MESS.Data.Models;
using MESS.Services.DTOs.PartDefinitions;
using MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.File;

namespace MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.Form;

/// <summary>
/// Provides extension methods to map between <see cref="PartNode"/> entities
/// and <see cref="PartNodeFormDTO"/> objects for create/update operations.
/// </summary>
public static class PartNodeFormDTOMapper
{
    /// <summary>
    /// Converts a <see cref="PartNode"/> entity to a <see cref="PartNodeFormDTO"/>.
    /// </summary>
    /// <param name="entity">The <see cref="PartNode"/> entity to convert.</param>
    /// <param name="clientId">
    /// A unique client-generated identifier used to track nodes in the UI
    /// before they are persisted.
    /// </param>
    /// <returns>
    /// A <see cref="PartNodeFormDTO"/> containing the node metadata and the
    /// associated part's name and number for editing in the UI.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <see cref="PartDefinition"/> navigation property was not loaded.
    /// </exception>
    /// <remarks>
    /// The underlying database model references a <see cref="PartDefinition"/>,
    /// but the form DTO stores only the part's name and number to keep the UI
    /// decoupled from database entities.
    /// </remarks>
    public static PartNodeFormDTO ToFormDTO(this PartNode entity, Guid clientId)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        if (entity.PartDefinition is null)
            throw new InvalidOperationException(
                $"PartDefinition was not loaded for PartNode {entity.Id}");

        return new PartNodeFormDTO
        {
            Id = entity.Id,
            ClientId = clientId,
            Position = entity.Position,
            NodeType = entity.NodeType,
            InputType = entity.InputType,
            Name = entity.PartDefinition.Name,
            Number = entity.PartDefinition.Number,
            IsSerialNumberUnique = entity.PartDefinition.IsSerialNumberUnique,
        };
    }
    
        
    /// <summary>
    /// Converts a <see cref="PartNodeFormDTO"/> to a file-safe <see cref="PartNodeFileDTO"/> for export.
    /// </summary>
    /// <param name="form">The form DTO representing the part node in the UI.</param>
    /// <returns>
    /// <see cref="PartNodeFileDTO"/> containing the part name, part number,
    /// position, and input type suitable for serialization to a work instruction file.
    /// </returns>
    /// <remarks>
    /// This method performs a direct mapping because the form DTO already stores
    /// the exported part name and number independently of any database entity.
    /// </remarks>
    public static PartNodeFileDTO ToFileDTO(this PartNodeFormDTO form)
    {
        if (form is null)
            throw new ArgumentNullException(nameof(form));

        return new PartNodeFileDTO
        {
            Position = form.Position,
            NodeType = form.NodeType,
            PartName = form.Name,
            PartNumber = form.Number,
            InputType = form.InputType,
            IsSerialNumberUnique =  form.IsSerialNumberUnique,
        };
    }

    /// <summary>
    /// Converts a <see cref="PartNodeFormDTO"/> to a new <see cref="PartNode"/> entity.
    /// </summary>
    /// <param name="dto">The <see cref="PartNodeFormDTO"/> to convert.</param>
    /// <returns>
    /// A <see cref="PartNode"/> entity containing the node metadata.
    /// The <see cref="PartDefinition"/> relationship is not resolved here
    /// and must be assigned during the save process.
    /// </returns>
    /// <remarks>
    /// The form DTO stores only the part name and number. Resolution or creation
    /// of the corresponding <see cref="PartDefinition"/> must occur in the
    /// application service responsible for persisting the work instruction.
    /// </remarks>
    public static PartNode ToNewEntity(this PartNodeFormDTO dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        return new PartNode
        {
            Id = dto.Id,
            Position = dto.Position,
            NodeType = dto.NodeType,
            InputType = dto.InputType,

            PartDefinition = new PartDefinition
            {
                Name = dto.Name,
                Number = dto.Number,
                IsSerialNumberUnique = dto.IsSerialNumberUnique
            }
        };
    }

    /// <summary>
    /// Converts a collection of <see cref="PartNodeFormDTO"/> DTOs to a list of <see cref="PartNode"/> entities.
    /// </summary>
    public static List<PartNode> ToEntityList(this IEnumerable<PartNodeFormDTO> dtos)
        => dtos.Select(d => d.ToNewEntity()).ToList();
}
