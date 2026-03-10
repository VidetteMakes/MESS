namespace MESS.Services.CRUD.Locations;


/// <summary>
/// Provides functionality to generate sequential location names
/// based on a specified numbering scheme.
/// </summary>
public static class LocationNameGenerator
{
    /// <summary>
    /// Generates a sequence of location names starting from a given index,
    /// according to the specified numbering scheme.
    /// </summary>
    /// <param name="scheme">The numbering scheme to use for generating names.</param>
    /// <param name="start">The starting index for generation. Zero-based.</param>
    /// <param name="count">The number of location names to generate.</param>
    /// <returns>An <see cref="IEnumerable{String}"/> containing the generated location names.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if an unknown numbering scheme is provided.</exception>
    public static IEnumerable<string> Generate(LocationNumberingScheme scheme, int start, int count)
    {
        switch (scheme)
        {
            case LocationNumberingScheme.Decimal:
                for (int i = start; i < start + count; i++)
                    yield return i.ToString();
                break;
            case LocationNumberingScheme.Hexadecimal:
                for (int i = start; i < start + count; i++)
                    yield return i.ToString("X"); // Uppercase hex
                break;
            case LocationNumberingScheme.Alphanumeric:
                for (int i = start; i < start + count; i++)
                    yield return ToAlpha(i);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null);
        }
    }

    /// <summary>
    /// Converts a zero-based number into an alphabetical string (e.g., 0 -> "A", 25 -> "Z", 26 -> "AA").
    /// </summary>
    /// <param name="number">The zero-based number to convert.</param>
    /// <returns>A string representing the number in alphabetical sequence.</returns>
    private static string ToAlpha(int number)
    {
        // Converts 1 -> A, 2 -> B ... 26 -> Z, 27 -> AA, etc.
        string result = "";
        number++; // Make 0-based index 1 -> A
        while (number > 0)
        {
            number--;
            result = (char)('A' + number % 26) + result;
            number /= 26;
        }
        return result;
    }
}