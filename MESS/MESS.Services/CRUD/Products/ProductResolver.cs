using MESS.Data.Context;
using MESS.Data.Models;
using Microsoft.EntityFrameworkCore;
using MESS.Services.CRUD.PartDefinitions;

namespace MESS.Services.CRUD.Products;

/// <inheritdoc />
public class ProductResolver : IProductResolver
{
    private readonly IPartDefinitionResolver _partDefinitionResolver;
    
    /// <summary>
    /// Constructs a Product Resolver that can use a Part Definition Resolver
    /// </summary>
    /// <param name="partDefinitionResolver"></param>
    public ProductResolver(IPartDefinitionResolver partDefinitionResolver)
    {
        _partDefinitionResolver = partDefinitionResolver;
    }

    /// <inheritdoc />
    public async Task<List<Product>> ResolveProductsAsync(
        ApplicationContext context,
        IEnumerable<string> productNames)
    {
        // Normalize input
        var normalizedNames = productNames
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedNames.Count == 0)
            return new List<Product>();

        // Load existing products + part definitions for the names provided
        var existingProducts = await context.Products
            .Include(p => p.PartDefinition)
            .Where(p => normalizedNames.Contains(p.PartDefinition.Name))
            .ToListAsync();

        // Create a lookup for already existing products by part name
        var productLookup = existingProducts
            .GroupBy(p => p.PartDefinition.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var results = new List<Product>();

        foreach (var name in normalizedNames)
        {
            if (!productLookup.TryGetValue(name, out var product))
            {
                var part = await _partDefinitionResolver.ResolveAsync(context, name, null)
                           ?? throw new InvalidOperationException($"Cannot resolve PartDefinition for '{name}'.");

                product = new Product
                {
                    PartDefinition = part,
                    IsActive = true
                };

                context.Products.Add(product);
                productLookup[name] = product;
            }

            results.Add(product);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<(List<Product> Resolved, List<string> Missing)> LookupProductsAsync(
        ApplicationContext context,
        IEnumerable<string> productNames)
    {
        var normalizedNames = productNames
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resolved = new List<Product>();
        var missing = new List<string>();

        if (normalizedNames.Count == 0)
            return (resolved, missing);

        var upperNames = normalizedNames.Select(n => n.ToUpperInvariant()).ToList();

        var existingProducts = await context.Products
            .Include(p => p.PartDefinition)
            .Where(p => upperNames.Contains(p.PartDefinition.Name.ToUpper()))
            .ToListAsync();

        var productLookup = existingProducts
            .GroupBy(p => p.PartDefinition.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var name in normalizedNames)
        {
            if (productLookup.TryGetValue(name, out var product))
                resolved.Add(product);
            else
                missing.Add(name);
        }

        return (resolved, missing);
    }
}