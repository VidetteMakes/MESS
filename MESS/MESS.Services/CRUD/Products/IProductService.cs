using MESS.Services.DTOs.Products.Detail;
using MESS.Services.DTOs.Products.Summary;

namespace MESS.Services.CRUD.Products;

using Data.Models;

/// <summary>
/// Interface for managing product-related operations in the database.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Designates an existing <see cref="PartDefinition"/> as a <see cref="Product"/> by creating a
    /// new <see cref="Product"/> row referencing it (with <c>IsActive = true</c> and no associated
    /// work instructions). Does <b>not</b> create a PartDefinition — the part must already exist.
    /// </summary>
    /// <param name="partDefinitionId">The ID of the existing part to designate.</param>
    /// <returns>The ID of the newly created product.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the part does not exist, or is already designated as a product.
    /// </exception>
    Task<int> DesignateAsProductAsync(int partDefinitionId);

    /// <summary>
    /// Finds a product by its unique ID, including related WorkInstructions and WorkStations.
    /// </summary>
    /// <param name="id">The ID of the product to find.</param>
    /// <returns>The product if found; otherwise, null.</returns>
    Task<Product?> GetByIdAsync(int id);

    /// <summary>
    /// Finds the 1st product with the given title.
    /// If given a version string it will find the first Product that has the
    /// given Title and Version match.
    /// Includes related WorkInstructions and WorkStations.
    /// </summary>
    /// <param name="title">The ID of the product to find.</param>
    /// <returns>The product if found; otherwise, null.</returns>
    Task<Product?> GetByTitleAsync(string title);
    
    /// <summary>
    /// Retrieves all products from the database, including related WorkInstructions and WorkStations.
    /// </summary>
    /// <returns>A list of all products.</returns>
    Task<IEnumerable<Product>> GetAllAsync();
    
    /// <summary>
    /// Asynchronously retrieves all products as lightweight summary DTOs,
    /// suitable for display in lists or selection controls.
    /// </summary>
    /// <returns>
    /// A collection of <see cref="ProductSummaryDTO"/> objects representing all products.
    /// Each DTO contains the product ID, name, number, active status, and associated part definition ID.
    /// </returns>
    /// <remarks>
    /// Only the <see cref="Product.PartDefinition"/> relationship is loaded to populate
    /// the DTO's Name and Number fields. This method is optimized for read-only projections.
    /// Exceptions during retrieval are caught and logged, with an empty list returned on failure.
    /// </remarks>
    Task<IEnumerable<ProductSummaryDTO>> GetAllSummariesAsync();

    /// <summary>
    /// Retrieves all products from the database along with their associated
    /// part definitions and work instructions, and maps them to
    /// <see cref="ProductDetailDTO"/> instances.
    /// </summary>
    /// <returns>
    /// A task that resolves to an <see cref="IEnumerable{ProductDetailDTO}"/> containing
    /// all products with full detail information. If an exception occurs,
    /// an empty list is returned and a warning is logged.
    /// </returns>
    public Task<IEnumerable<ProductDetailDTO>> GetAllDetailsAsync();

    /// <summary>
    /// Updates an existing product's <see cref="Product.IsActive"/> flag and its WorkInstruction
    /// associations. Part details (<see cref="PartDefinition.Name"/> / <see cref="PartDefinition.Number"/>)
    /// are owned by Part Management and may not be changed here.
    /// </summary>
    /// <param name="product">The product to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied product attempts to change the underlying PartDefinition's Name or Number.
    /// </exception>
    Task UpdateProductAsync(Product product);
    
    /// <summary>
    /// Asynchronously updates an existing product using the provided <see cref="ProductDetailDTO"/>.
    /// </summary>
    /// <param name="dto">
    /// The detailed product DTO containing the updated product data, including
    /// its identifier, associated PartDefinition information, and related WorkInstructions.
    /// </param>
    /// <remarks>
    /// The implementation is expected to:
    /// <list type="bullet">
    /// <item>
    /// <description>Locate the existing Product entity by <see cref="ProductDetailDTO.ProductId"/>.</description>
    /// </item>
    /// <item>
    /// <description>Update scalar properties such as <c>IsActive</c>.</description>
    /// </item>
    /// <item>
    /// <description>Update the associated PartDefinition fields (e.g., Number and Name).</description>
    /// </item>
    /// <item>
    /// <description>Synchronize the product's WorkInstructions based on the DTO.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dto"/> is <c>null</c>.
    /// </exception>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous update operation.
    /// </returns>
    Task UpdateProductAsync(ProductDetailDTO dto);

    /// <summary>
    /// Removes a product from the database by its unique ID.
    /// </summary>
    /// <param name="id">The ID of the product to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteByIdAsync(int id);
}