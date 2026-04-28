using MESS.Services.UI.PartTraceability;

namespace MESS.Services.CRUD.PartTraceability;

/// <summary>
/// Defines operations for reconstructing part traceability snapshots from existing serialized parts
/// using scanned tag codes and a work instruction structure.
/// </summary>
public interface IPartTraceabilityReworkService
{
    /// <summary>
    /// Builds a collection of <see cref="PartTraceabilitySnapshot"/> instances from the provided tag codes.
    /// Each tag code represents a produced (root) part for a log entry. The method traverses the underlying
    /// serialized part hierarchy and populates child entries based on the structure defined by the specified
    /// work instruction.
    /// </summary>
    /// <param name="tagCodes">
    /// A list of tag codes corresponding to produced parts. Each tag maps to a single snapshot/log index.
    /// </param>
    /// <param name="workInstructionId">
    /// The identifier of the work instruction used to determine which part nodes should be included
    /// in the resulting snapshots.
    /// </param>
    /// <returns>
    /// A list of <see cref="PartTraceabilitySnapshot"/> objects, one per valid tag code,
    /// representing reconstructed traceability data.
    /// </returns>
    Task<List<PartTraceabilitySnapshot>> BuildSnapshotsFromTagCodesAsync(
        List<string> tagCodes,
        int workInstructionId);
    
    /// <summary>
    /// Validates that a scanned tag code corresponds to the expected produced part
    /// for a given work instruction.
    /// </summary>
    /// <param name="tagCode">The scanned tag code to validate.</param>
    /// <param name="workInstructionId">The ID of the work instruction to validate against.</param>
    /// <returns>
    /// A tuple containing:
    /// <c>IsValid</c> – <c>true</c> if the tag code matches the expected produced part; otherwise <c>false</c>.<br/>
    /// <c>ErrorMessage</c> – a descriptive error message if validation fails, or <c>null</c> if valid.
    /// </returns>
    /// <remarks>
    /// Validation checks include:
    /// <list type="bullet">
    ///   <item><description>Tag code is not empty or whitespace.</description></item>
    ///   <item><description>Serialized part exists for the provided tag code.</description></item>
    ///   <item><description>Work instruction exists for the provided ID.</description></item>
    ///   <item><description>Work instruction has a defined produced part.</description></item>
    ///   <item><description>Serialized part's definition matches the expected produced part of the work instruction.</description></item>
    /// </list>
    /// </remarks>
    Task<(bool IsValid, string? ErrorMessage)> ValidateProducedPartAsync(string tagCode, int workInstructionId);
}