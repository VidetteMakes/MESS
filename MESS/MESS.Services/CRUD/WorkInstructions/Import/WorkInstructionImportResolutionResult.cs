namespace MESS.Services.CRUD.WorkInstructions.Import;

/// <summary>
/// Aggregates every missing reference found while resolving an imported (or newly authored)
/// work instruction against the database. Collected together so the admin can resolve all
/// missing parts/products in a single pass rather than one error at a time.
/// </summary>
public class WorkInstructionImportResolutionResult
{
    /// <summary>Part names (produced part) that do not match any existing <c>PartDefinition</c>.</summary>
    public List<string> MissingPartDefinitions { get; } = [];

    /// <summary>Product names that do not match any existing <c>Product</c>.</summary>
    public List<string> MissingProducts { get; } = [];

    /// <summary>
    /// PartNode parts that do not exist, paired with a context label identifying where in the
    /// instruction the reference appears.
    /// </summary>
    public List<(string stepName, string partName)> MissingPartNodeParts { get; } = [];

    /// <summary>True when any category contains at least one missing reference.</summary>
    public bool HasErrors =>
        MissingPartDefinitions.Count > 0
        || MissingProducts.Count > 0
        || MissingPartNodeParts.Count > 0;
}
