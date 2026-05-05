using MESS.Data.Models;
using MESS.Services.DTOs.Products.Summary;
using MESS.Services.DTOs.WorkInstructions.File;
using MESS.Services.DTOs.WorkInstructions.Nodes.Form;
using MESS.Services.DTOs.WorkInstructions.Summary;

namespace MESS.Services.DTOs.WorkInstructions.Form;

/// <summary>
/// Provides extension methods for mapping between
/// <see cref="WorkInstruction"/> entities and
/// <see cref="WorkInstructionFormDTO"/> objects.
/// </summary>
/// <remarks>
/// This mapper delegates node-level conversions to
/// <see cref="WorkInstructionNodeFormMapper"/> to avoid redundancy.
/// </remarks>
public static class WorkInstructionFormDTOMapper
{
    /// <summary>
    /// Converts a <see cref="WorkInstruction"/> entity into a <see cref="WorkInstructionFormDTO"/>.
    /// </summary>
    /// <param name="entity">The work instruction entity to convert.</param>
    /// <returns>A <see cref="WorkInstructionFormDTO"/> representation of the entity.</returns>
    public static WorkInstructionFormDTO ToFormDTO(this WorkInstruction entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new WorkInstructionFormDTO
        {
            Id = entity.Id,
            Title = entity.Title,
            Version = entity.Version,
            OriginalId = entity.OriginalId,
            IsLatest = entity.IsLatest,
            IsActive = entity.IsActive,
            ShouldGenerateQrCode = entity.ShouldGenerateQrCode,
            PartProducedIsSerialized = entity.PartProducedIsSerialized,
            ProducedPartName = entity.PartProduced?.Name,
            ProductNames = entity.Products.Select(p => p.PartDefinition.Name).ToList(),
            Nodes = entity.Nodes.Select(n => n.ToFormDTO(Guid.NewGuid())).ToList()
        };
    }

    /// <param name="dto">The form DTO to convert.</param>
    extension(WorkInstructionFormDTO dto)
    {
        /// <summary>
        /// Converts a <see cref="WorkInstructionFormDTO"/> into a <see cref="WorkInstruction"/> entity.
        /// </summary>
        /// <returns>A <see cref="WorkInstruction"/> entity populated from the DTO.</returns>
        public WorkInstruction ToNewEntity()
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new WorkInstruction
            {
                Id = dto.Id ?? 0,
                Title = dto.Title,
                Version = dto.Version,
                OriginalId = dto.OriginalId,
                IsLatest = dto.IsLatest,
                IsActive = dto.IsActive,
                ShouldGenerateQrCode = dto.ShouldGenerateQrCode,
                PartProducedIsSerialized = dto.PartProducedIsSerialized,
                // PartProduced and Products are resolved separately.
                Nodes = dto.Nodes.Select(n => n.ToNewEntity()).ToList()
            };
        }

        /// <summary>
        /// Maps a <see cref="WorkInstructionFormDTO"/> to a <see cref="WorkInstructionSummaryDTO"/>.
        /// </summary>
        /// <param name="allProducts">
        /// A collection of <see cref="ProductSummaryDTO"/> used to populate the
        /// <see cref="WorkInstructionSummaryDTO.Products"/> property.
        /// Only products whose titles match <see cref="WorkInstructionFormDTO.ProductNames"/> will be included.
        /// </param>
        /// <returns>A <see cref="WorkInstructionSummaryDTO"/> containing the mapped summary data.</returns>
        public WorkInstructionSummaryDTO ToSummaryDTO(IEnumerable<ProductSummaryDTO> allProducts)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(allProducts);

            return new WorkInstructionSummaryDTO
            {
                Id = dto.Id ?? 0,
                Title = dto.Title,
                Version = dto.Version,
                OriginalId = dto.OriginalId,
                IsLatest = dto.IsLatest,
                IsActive = dto.IsActive,
                PartProducedName = dto.ProducedPartName,
                Products = allProducts
                    .Where(p => dto.ProductNames.Contains(p.Name))
                    .ToList()
            };
        }

        /// <summary>
        /// Converts a <see cref="WorkInstructionFormDTO"/> (editable form DTO)
        /// to a <see cref="WorkInstructionFileDTO"/> for file export.
        /// </summary>
        public WorkInstructionFileDTO ToFileDTO()
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new WorkInstructionFileDTO
            {
                Title = dto.Title,
                Version = dto.Version,
                ShouldGenerateQrCode = dto.ShouldGenerateQrCode,
                PartProducedIsSerialized = dto.PartProducedIsSerialized,
                ProducedPartName = dto.ProducedPartName,
                AssociatedProductNames = dto.ProductNames.ToList(),
                Nodes = dto.Nodes
                    .Select(n => n.ToFileDTO())
                    .ToList()
            };
        }
    }
}