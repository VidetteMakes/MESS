using MESS.Data.Models;
using MESS.Services.DTOs.ProductionLogs.Form;

namespace MESS.Services.CRUD.ProductionLogParts;

/// <summary>
/// Interface for managing part entry during production log creation. Provides methods and events
/// for managing production log parts and product numbers.
/// </summary>
public interface IProductionLogPartService
{
    /// <summary>
    /// Event triggered when the current product number changes.
    /// </summary>
    public event Action? CurrentProductNumberChanged;

    /// <summary>
    /// Gets or sets the current product number.
    /// </summary>
    public string? CurrentProductNumber { get; set; }
    
    /*
    /// <summary>
    /// Saves all in-memory <see cref="ProductionLogPart"/> entries to the database for the given list of saved production logs.
    /// Each part will be assigned the corresponding <see cref="ProductionLog"/> ID before persistence.
    /// </summary>
    /// <param name="savedLogs">The list of production logs that have been saved, mapped by index.</param>
    /// <returns><c>true</c> if all parts were saved successfully; otherwise, <c>false</c>.</returns>
    Task<bool> SaveAllLogPartsAsync(List<ProductionLogFormDTO> savedLogs);
    */
    
    /// <summary>
    /// Retrieves all SerialNumberLogs Async
    /// </summary>
    /// <returns>Nullable List of SerialNumberLogs</returns>
    public Task<List<ProductionLogPart>?> GetAllAsync();
    
    /// <summary>
    /// Creates and saves a new ProductionLogPart in the database.
    /// </summary>
    /// <param name="productionLogPart">The ProductionLogPart object to be saved in the database</param>
    /// <returns>True value if the operation succeeded, false otherwise</returns>
    public Task<bool> CreateAsync(ProductionLogPart productionLogPart);

    /// <summary>
    /// Creates and saves a List of SerialNumberLogs in the database.
    /// </summary>
    /// <param name="productionLogParts"></param>
    /// <returns>True value if the operation succeeded, false otherwise</returns>
    public Task<bool> CreateRangeAsync(List<ProductionLogPart> productionLogParts);
    /// <summary>
    /// Updates the saved ProductionLogPart in the database.
    /// </summary>
    /// <param name="productionLogPart"></param>
    /// <returns>True value if the operation succeeded, false otherwise</returns>
    public Task<bool> UpdateAsync(ProductionLogPart productionLogPart);
    
    /// <summary>
    /// Asynchronously deletes a <see cref="ProductionLogPart"/> identified by its composite key.
    /// </summary>
    /// <param name="productionLogId">The ID of the <see cref="ProductionLog"/> associated with the part entry.</param>
    /// <param name="serializablePartId">The ID of the <see cref="SerializablePart"/> linked to this log entry.</param>
    /// <param name="operationType">
    /// The type of part operation that uniquely identifies the record 
    /// (e.g., <see cref="PartOperationType.Produced"/>, <see cref="PartOperationType.Installed"/>, or <see cref="PartOperationType.Removed"/>).
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous delete operation.  
    /// The task result is <c>true</c> if the deletion succeeded or the record existed and was removed;  
    /// otherwise, <c>false</c> if no matching record was found or an exception occurred.
    /// </returns>
    Task<bool> DeleteAsync(int productionLogId, int serializablePartId, PartOperationType operationType);
    
    /// <summary>
    /// Retrieves the identifiers of all serialized parts that are currently nested
    /// within the specified root serialized part, including the root itself.
    /// </summary>
    /// <param name="rootSerializablePartId">
    /// The identifier of the root serialized part whose assembly hierarchy should be retrieved.
    /// </param>
    /// <returns>
    /// A list of serialized part identifiers representing the current assembly structure.
    /// </returns>
    Task<List<int>> GetCurrentAssemblyPartIdsAsync(int rootSerializablePartId);
}