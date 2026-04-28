namespace MESS.Data.Models;

/// <summary>
/// Represents an "Action" or node in a work instruction that is associated with a single part.
/// </summary>
public class PartNode : WorkInstructionNode
{
    /// <summary>
    /// Gets or sets the foreign key of the part associated with this node.
    /// </summary>
    public int PartDefinitionId { get; set; }

    /// <summary>
    /// Gets or sets the part associated with this node.
    /// </summary>
    public PartDefinition? PartDefinition { get; set; }
}

/// <summary>
/// Defines the type of input expected by a <see cref="PartNode"/> (stored on <see cref="PartDefinition"/>).
/// </summary>
/// <remarks>
/// This enumeration determines how a user or system should provide
/// identifying information for a part when completing a work instruction step.
/// </remarks>
public enum PartInputType
{
    /// <summary>
    /// Indicates that the part node expects a unique serial number
    /// associated with a <see cref="SerializablePart"/>.
    /// </summary>
    SerialNumber = 0,
    
    /// <summary>
    /// Indicates that the part node expects a scanned or manually entered
    /// tag code corresponding to a <see cref="Tag"/>. The tag is used to
    /// identify the associated <see cref="SerializablePart"/> for traceability.
    /// </summary>
    Tag = 1,
}

