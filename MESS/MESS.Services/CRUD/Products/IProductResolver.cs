using MESS.Data.Context;
using MESS.Data.Models;

namespace MESS.Services.CRUD.Products;

/// <summary>
/// Provides functionality for resolving product names into <see cref="Product"/> entities.
/// </summary>
/// <remarks>
/// This resolver is responsible for locating existing <see cref="Product"/> records
/// based on provided product names and creating any missing <see cref="PartDefinition"/>
/// and <see cref="Product"/> entities as needed. It ensures that the returned products
/// are tracked by the supplied <see cref="ApplicationContext"/> so they can be safely
/// associated with other aggregates before saving changes.
/// </remarks>
public interface IProductResolver
{
    /// <summary>
    /// Resolves a collection of product names into corresponding <see cref="Product"/> entities.
    /// </summary>
    /// <param name="context">
    /// The active <see cref="ApplicationContext"/> used for querying existing records
    /// and tracking any newly created entities.
    /// </param>
    /// <param name="productNames">
    /// The collection of product names to resolve. Names are typically trimmed and
    /// normalized before being passed to this method.
    /// </param>
    /// <returns>
    /// A list of <see cref="Product"/> entities corresponding to the supplied product names.
    /// Existing products will be reused, while missing products and their associated
    /// <see cref="PartDefinition"/> records will be created and tracked by the context.
    /// </returns>
    Task<List<Product>> ResolveProductsAsync(
        ApplicationContext context,
        IEnumerable<string> productNames);

    /// <summary>
    /// Looks up existing <see cref="Product"/> entities by part name without creating any
    /// missing <see cref="Product"/> or <see cref="PartDefinition"/> rows.
    /// </summary>
    /// <param name="context">The active context used for the lookup.</param>
    /// <param name="productNames">Product names to resolve (matched case-insensitively by part name).</param>
    /// <returns>
    /// A tuple of the resolved products and the list of names that could not be matched to a product.
    /// </returns>
    Task<(List<Product> Resolved, List<string> Missing)> LookupProductsAsync(
        ApplicationContext context,
        IEnumerable<string> productNames);
}