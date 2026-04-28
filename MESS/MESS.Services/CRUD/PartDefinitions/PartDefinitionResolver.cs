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
        string? number)
    {
        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            return null;

        var normalizedNumber = string.IsNullOrWhiteSpace(number)
            ? null
            : number.Trim();

        var upperName = normalizedName.ToUpperInvariant();

        var existing = context.PartDefinitions
                           .Local
                           .FirstOrDefault(p =>
                               p.Name.ToUpper() == upperName)
                       ?? await context.PartDefinitions
                           .FirstOrDefaultAsync(p =>
                               p.Name.ToUpper() == upperName);

        if (existing != null)
        {
            if (existing.Number == null && normalizedNumber != null)
                existing.Number = normalizedNumber;

            return existing;
        }

        var newPart = new PartDefinition
        {
            Name = normalizedName,
            Number = normalizedNumber,
            IsSerialNumberUnique = true,
            InputType = PartInputType.SerialNumber
        };

        context.PartDefinitions.Add(newPart);
        return newPart;
    }
}
