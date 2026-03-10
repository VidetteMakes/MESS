namespace MESS.Services.CRUD.Locations;


/// <summary>
/// Specifies the numbering scheme to use when generating location names in bulk.
/// </summary>
public enum LocationNumberingScheme
{
    /// <summary>
    /// Generate location names using decimal numbers (0, 1, 2, ...).
    /// </summary>
    Decimal,

    /// <summary>
    /// Generate location names using hexadecimal numbers (0, 1, ..., 9, A, B, ..., F, 10, ...).
    /// </summary>
    Hexadecimal,

    /// <summary>
    /// Generate location names using a full alphanumeric sequence (0-9, A-Z).
    /// This can produce names like 0, 1, ..., 9, A, B, ..., Z, 10, 11, ..., etc.
    /// </summary>
    Alphanumeric
}
