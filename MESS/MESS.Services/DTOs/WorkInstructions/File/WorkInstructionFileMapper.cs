using MESS.Data.Models;
using MESS.Services.DTOs.WorkInstructions.Form;
using MESS.Services.DTOs.WorkInstructions.Nodes.File;
using MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.File;
using MESS.Services.DTOs.WorkInstructions.Nodes.StepNodes.File;

namespace MESS.Services.DTOs.WorkInstructions.File;

/// <summary>
/// Provides mapping extensions between <see cref="WorkInstruction"/> 
/// and <see cref="WorkInstructionFileDTO"/>.
/// </summary>
public static class WorkInstructionFileMapper
{
    /// <summary>
    /// Converts a <see cref="WorkInstruction"/> entity to a <see cref="WorkInstructionFileDTO"/>.
    /// </summary>
    public static WorkInstructionFileDTO ToFileDTO(this WorkInstruction entity)
    {
        return new WorkInstructionFileDTO
        {
            Title = entity.Title,
            Version = entity.Version,
            ShouldGenerateQrCode = entity.ShouldGenerateQrCode,
            PartProducedIsSerialized = entity.PartProducedIsSerialized,
            ProducedPartName = entity.PartProduced?.Name,
            AssociatedProductNames = entity.Products
                .Select(p => p.PartDefinition.Name)
                .ToList(),

            Nodes = entity.Nodes
                .OrderBy(n => n.Position)
                .Select(n => n switch
                {
                    Step step => (WorkInstructionNodeFileDTO)step.ToFileDTO(),
                    PartNode part => (WorkInstructionNodeFileDTO)part.ToFileDTO(),
                    _ => throw new NotSupportedException(
                        $"Unsupported node type: {n.GetType().Name}")
                })
                .ToList()
        };
    }
    
    /// <summary>
    /// Converts a <see cref="WorkInstructionFileDTO"/> into a <see cref="WorkInstructionFormDTO"/>,
    /// which is suitable for use in the Blazor UI or for editing in the application.
    /// </summary>
    /// <param name="fileDto">The file DTO representing a work instruction, typically imported from an external source.</param>
    /// <returns>
    /// A new <see cref="WorkInstructionFormDTO"/> containing all relevant properties from the file DTO,
    /// including title, version, flags, produced part name, product names, and ordered nodes mapped to their form DTOs.
    /// </returns>
    /// <remarks>
    /// Nodes from the file DTO are ordered by <see cref="WorkInstructionNodeFileDTO.Position"/>
    /// and converted individually using <c>ToFormDTO()</c>. Product names are copied from
    /// <see cref="WorkInstructionFileDTO.AssociatedProductNames"/>. This method does not modify
    /// the original <paramref name="fileDto"/>.
    /// </remarks>
    public static WorkInstructionFormDTO ToFormDTO(this WorkInstructionFileDTO fileDto)
    {
        if (fileDto == null)
            throw new ArgumentNullException(nameof(fileDto));

        return new WorkInstructionFormDTO
        {
            Title = fileDto.Title,
            Version = fileDto.Version,
            ShouldGenerateQrCode = fileDto.ShouldGenerateQrCode,
            PartProducedIsSerialized = fileDto.PartProducedIsSerialized,

            ProducedPartName = fileDto.ProducedPartName,

            ProductNames = fileDto.AssociatedProductNames?
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList() ?? [],

            Nodes = fileDto.Nodes
                .OrderBy(n => n.Position)
                .Select(n => n.ToFormDTO())
                .ToList()
        };
    }
}