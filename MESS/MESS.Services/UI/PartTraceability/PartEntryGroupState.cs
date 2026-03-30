namespace MESS.Services.UI.PartTraceability;

/// <summary>
/// Represents a group of <see cref="PartEntryState"/> instances associated with a single production log.
/// This model is used to organize part entry UI state by log index during batch or multi-log workflows.
/// </summary>
public class PartEntryGroupState
{
    /// <summary>
    /// Gets or sets the index of the production log this group represents.
    /// This is used to distinguish between multiple logs in batch scenarios.
    /// </summary>
    public int LogIndex { get; set; }

    private readonly Dictionary<int, PartEntryState> _entries = new();

    /// <summary>
    /// Gets the collection of part entry states associated with this log,
    /// keyed by the corresponding part node identifier.
    /// </summary>
    /// <remarks>
    /// The key represents the <see cref="PartEntryState.PartNodeId"/>,
    /// allowing efficient lookup of entries by part node.
    /// </remarks>
    public IReadOnlyDictionary<int, PartEntryState> Entries => _entries;

    /// <summary>
    /// Retrieves the <see cref="PartEntryState"/> associated with the specified part node identifier.
    /// </summary>
    /// <param name="partNodeId">The identifier of the part node.</param>
    /// <returns>
    /// The matching <see cref="PartEntryState"/> if found; otherwise, <c>null</c>.
    /// </returns>
    public PartEntryState? GetEntry(int partNodeId)
    {
        _entries.TryGetValue(partNodeId, out var entry);
        return entry;
    }

    /// <summary>
    /// Retrieves the <see cref="PartEntryState"/> associated with the specified part node identifier.
    /// Throws an exception if the entry does not exist.
    /// </summary>
    /// <param name="partNodeId">The identifier of the part node.</param>
    /// <returns>The matching <see cref="PartEntryState"/>.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no entry exists for the specified part node identifier.
    /// </exception>
    public PartEntryState GetRequiredEntry(int partNodeId)
    {
        return _entries.TryGetValue(partNodeId, out var entry)
            ? entry
            : throw new KeyNotFoundException($"No PartEntryState found for PartNodeId {partNodeId}.");
    }

    /// <summary>
    /// Adds a new <see cref="PartEntryState"/> to the group.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if the entry is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if an entry with the same PartNodeId already exists.
    /// </exception>
    public void AddEntry(PartEntryState entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!_entries.TryAdd(entry.PartNodeId, entry))
        {
            throw new InvalidOperationException(
                $"An entry for PartNodeId {entry.PartNodeId} already exists in this group.");
        }
    }

    /// <summary>
    /// Adds or replaces a <see cref="PartEntryState"/> for the specified part node identifier.
    /// </summary>
    /// <param name="entry">The entry to add or update.</param>
    public void UpsertEntry(PartEntryState entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries[entry.PartNodeId] = entry;
    }

    /// <summary>
    /// Removes the entry associated with the specified part node identifier.
    /// </summary>
    /// <param name="partNodeId">The identifier of the part node.</param>
    /// <returns>
    /// <c>true</c> if the entry was removed; otherwise, <c>false</c>.
    /// </returns>
    public bool RemoveEntry(int partNodeId)
    {
        return _entries.Remove(partNodeId);
    }

    /// <summary>
    /// Removes all entries from this group.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }
}