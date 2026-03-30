using MESS.Data.Context;
using MESS.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace MESS.Services.CRUD.PartDefinitions;

///<inheritdoc/>
public class PartDefinitionResolver : IPartDefinitionResolver
{
    ///<inheritdoc/>
    public async Task<PartDefinition?> ResolveAsync(
        ApplicationContext context,
        string? name,
        string? number,
        bool isSerialNumberUnique = true)
    {
        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            return null;

        var normalizedNumber = string.IsNullOrWhiteSpace(number)
            ? null
            : number.Trim();

        var upperName = normalizedName.ToUpperInvariant();

        // --- Step 1: Try to find existing by Name only ---
        var existing = context.PartDefinitions
                           .Local
                           .FirstOrDefault(p =>
                               p.Name.ToUpper() == upperName)
                       ?? await context.PartDefinitions
                           .FirstOrDefaultAsync(p =>
                               p.Name.ToUpper() == upperName);

        if (existing != null)
        {
            // --- Step 2: Check Serial Number uniqueness ---
            if (existing.IsSerialNumberUnique != isSerialNumberUnique)
            {
                throw new InvalidOperationException(
                    $"Part '{existing.Name}' already exists with IsSerialNumberUnique = {existing.IsSerialNumberUnique}, " +
                    $"but attempted to use {isSerialNumberUnique}. This cannot be changed from the Work Instruction editor.");
            }

            // --- Step 3: Fill in missing Number if needed ---
            if (existing.Number == null && normalizedNumber != null)
            {
                existing.Number = normalizedNumber;
            }

            return existing;
        }

        // --- Step 4: Create new if not found ---
        var newPart = new PartDefinition
        {
            Name = normalizedName,
            Number = normalizedNumber,
            IsSerialNumberUnique = isSerialNumberUnique
        };

        context.PartDefinitions.Add(newPart);
        return newPart;
    }
}