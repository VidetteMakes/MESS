using MESS.Data.Models;
using MESS.Services.DTOs.WorkInstructions.Nodes.File;

namespace MESS.Services.DTOs.WorkInstructions.Nodes.PartNodes.File;

/// <summary>
/// Represents a file-safe data transfer object for a part node within a work instruction.
/// 
/// This DTO contains sufficient business identity information to resolve
/// a corresponding <see cref="PartDefinition"/> during import.
/// </summary>
public class PartNodeFileDTO : WorkInstructionNodeFileDTO
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PartNodeFileDTO"/> class.
    /// Sets the node type to <see cref="WorkInstructionNodeType.Part"/>.
    /// </summary>
    public PartNodeFileDTO()
    {
        NodeType = WorkInstructionNodeType.Part;
    }

    /// <summary>
    /// Gets or sets the name of the associated part.
    /// This value is required and serves as the primary business identifier.
    /// </summary>
    public string PartName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional part number associated with the part.
    /// This value strengthens uniqueness during import resolution.
    /// </summary>
    public string? PartNumber { get; set; }
    
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
    /// Gets or sets the type of input expected for this part node.
    /// </summary>
    public PartInputType InputType { get; set; }
    
    /// <summary>
    /// Gets a serialized string representation of the part for import/export purposes.
    /// </summary>
    /// <remarks>
    /// The output format is positional and designed to be fully compatible with the part list parser.
    /// It supports the following forms:
    /// <code>
    /// PART_NAME
    /// (PART_NAME, PART_NUMBER)
    /// (PART_NAME, PART_NUMBER, INPUT_TYPE)
    /// (PART_NAME, PART_NUMBER, INPUT_TYPE, IS_SERIAL_UNIQUE)
    /// </code>
    /// 
    /// Values are included progressively and omitted when they match default assumptions:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="PartNumber"/> is omitted when null or whitespace, unless required to preserve positional alignment.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="InputType"/> is omitted when it is <see cref="PartInputType.SerialNumber"/> (the default).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="IsSerialNumberUnique"/> is omitted when <c>true</c> (the default).
    /// </description>
    /// </item>
    /// </list>
    /// 
    /// When a later value must be included but an earlier value is omitted, empty placeholders are inserted
    /// to preserve positional correctness for parsing (e.g., <c>(PartName, , , false)</c>).
    /// 
    /// If only the part name is present, the value is returned without parentheses.
    /// </remarks>
    public string PartExportString
    {
        get
        {
            var values = new List<string> { PartName };

            // Always preserve positional behavior
            if (!string.IsNullOrWhiteSpace(PartNumber))
                values.Add(PartNumber);
            else if (InputType != PartInputType.SerialNumber || !IsSerialNumberUnique)
                values.Add(string.Empty); // placeholder to preserve position

            if (InputType != PartInputType.SerialNumber)
                values.Add(InputType.ToString());
            else if (!IsSerialNumberUnique)
                values.Add(string.Empty); // placeholder for skipped inputType

            if (!IsSerialNumberUnique)
                values.Add(IsSerialNumberUnique.ToString().ToLower());

            return values.Count == 1
                ? PartName
                : $"({string.Join(", ", values)})";
        }
    }
}
