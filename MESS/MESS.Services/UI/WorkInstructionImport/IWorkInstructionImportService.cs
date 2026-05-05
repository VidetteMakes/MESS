using MESS.Services.DTOs.WorkInstructions.File;
using MESS.Services.DTOs.WorkInstructions.Form;

namespace MESS.Services.UI.WorkInstructionImport;

/// <summary>
/// Provides application-level logic for converting a parsed
/// <see cref="WorkInstructionFileDTO"/> into a <see cref="WorkInstructionFormDTO"/>
/// that can be edited within the UI.
/// </summary>
/// <remarks>
/// This service represents the second stage of the work instruction import pipeline.
/// The first stage parses an external file (such as an Excel spreadsheet) into a
/// <see cref="WorkInstructionFileDTO"/>. This service then converts that file DTO
/// into a <see cref="WorkInstructionFormDTO"/> suitable for display and editing
/// within the application.
///
/// Validation, entity resolution, and creation of missing parts or products are
/// expected to occur later during the work instruction save process.
///
/// This design allows users to correct import issues interactively rather than
/// failing the import prematurely.
/// </remarks>
public interface IWorkInstructionImportService
{
    /// <summary>
    /// Converts a parsed work instruction file DTO into an editable
    /// <see cref="WorkInstructionFormDTO"/>.
    /// </summary>
    /// <param name="fileDto">
    /// The work instruction data parsed from an external file such as an Excel spreadsheet.
    /// </param>
    /// <returns>
    /// A <see cref="WorkInstructionImportApplicationResult"/> containing the mapped
    /// <see cref="WorkInstructionFormDTO"/> if the import mapping succeeds.
    /// </returns>
    /// <remarks>
    /// This method performs a structural conversion from the file representation
    /// of a work instruction to the form representation used by the UI editor.
    ///
    /// No validation against the database is performed during this step. In particular:
    /// <list type="bullet">
    /// <item><description>Part definitions are not resolved or validated.</description></item>
    /// <item><description>Products referenced by the instruction are not verified.</description></item>
    /// <item><description>Missing or unknown parts are preserved rather than rejected.</description></item>
    /// </list>
    ///
    /// The returned form DTO may therefore contain unresolved or incomplete data.
    /// Users are expected to review and adjust the imported instruction in the UI
    /// before saving it. Database validation and creation of missing entities occur
    /// during the work instruction persistence workflow.
    /// </remarks>
    Task<WorkInstructionImportApplicationResult> ImportAsync(WorkInstructionFileDTO fileDto);
}