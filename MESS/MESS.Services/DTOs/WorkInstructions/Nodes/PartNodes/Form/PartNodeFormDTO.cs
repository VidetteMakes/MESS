using MESS.Data.Models;
using MESS.Services.DTOs.WorkInstructions.Nodes.Form;

namespace MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.Form;

/// <summary>
/// Represents a form DTO for editing or creating a <see cref="PartNode"/> in the Blazor UI.
/// </summary>
/// <remarks>
/// A part node represents a reference to a part required or produced during a work
/// instruction step. During editing and import, the part is represented only by its
/// identifying information (such as name and optional part number).
///
/// Resolution of the part to a <see cref="PartDefinition"/> entity occurs later
/// during the work instruction save process. If the referenced part does not yet
/// exist, it may be created at that time.
/// </remarks>
public class PartNodeFormDTO : WorkInstructionNodeFormDTO
{
    /// <summary>
    /// Gets or sets the human-readable name of the part referenced by this node.
    /// </summary>
    /// <remarks>
    /// This value typically originates from an imported file or user input and
    /// is used to resolve or create a corresponding <see cref="PartDefinition"/>
    /// when the work instruction is saved.
    /// </remarks>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional part number associated with the part.
    /// </summary>
    /// <remarks>
    /// This value may be used to uniquely identify the part within the system.
    /// If provided, it will be used during save operations to resolve or create
    /// the corresponding <see cref="PartDefinition"/>.
    /// </remarks>
    public string? Number { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether instances (serializable parts) of this part definition
    /// can be uniquely identified by their serial number.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, each serial number is expected to be unique for this part
    /// definition and may be used as a sole identifier for individual part instances.
    /// When <c>false</c>, serial numbers are not guaranteed to be unique and additional
    /// identifying information may be required.
    /// </remarks>
    public bool IsSerialNumberUnique { get; set; } = true;

    /// <summary>
    /// Indicates what kind of input this part node expects (for example,
    /// a serial number or a production log identifier).
    /// </summary>
    public PartInputType InputType { get; set; } = PartInputType.SerialNumber;
}