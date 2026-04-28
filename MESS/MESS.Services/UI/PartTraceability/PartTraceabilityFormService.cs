using System.Text;
using MESS.Services.CRUD.SerializableParts;
using MESS.Services.CRUD.Tags;

namespace MESS.Services.UI.PartTraceability;

/// <inheritdoc/>
public class PartTraceabilityFormService : IPartTraceabilityFormService 
{
    /// <inheritdoc/>
    public Action? OnStateChanged { get; set; }
    
    private readonly ISerializablePartService _serializablePartService;
    
    private readonly Dictionary<int, LogState> _logs = new();

    private readonly ITagService _tagService;
    
    /// <summary>
    /// Creates a new instance of <see cref="PartTraceabilityFormService"/>.
    /// </summary>
    /// <param name="serializablePartService">
    /// The service used to resolve tag codes to serializable parts.
    /// </param>
    /// <param name="tagService">
    /// The service used to check if tag codes are available (for produced parts)
    /// </param>
    public PartTraceabilityFormService(ISerializablePartService serializablePartService,  ITagService tagService)
    {
        _serializablePartService = serializablePartService;
        _tagService = tagService;
    }
    
    /// <inheritdoc/>
    public void Initialize(IEnumerable<int> logIndexes, IEnumerable<List<int>> nodeBlocks)
    {
        _logs.Clear();

        var blocks = nodeBlocks.ToList();

        foreach (var logIndex in logIndexes)
        {
            var logState = new LogState
            {
                LogIndex = logIndex
            };

            foreach (var block in blocks)
            {
                foreach (var partNodeId in block)
                {
                    if (!logState.Entries.ContainsKey(partNodeId))
                    {
                        logState.Entries[partNodeId] = new PartEntryState
                        {
                            PartNodeId = partNodeId
                        };
                    }
                }
            }

            _logs[logIndex] = logState;
        }
    }

    /// <inheritdoc/>
    public PartEntryState GetEntry(int logIndex, int partNodeId)
    {
        if (!_logs.TryGetValue(logIndex, out var log) ||
            !log.Entries.TryGetValue(partNodeId, out var entry))
        {
            throw new KeyNotFoundException(
                $"Entry not found for logIndex {logIndex}, partNodeId {partNodeId}");
        }

        return entry;
    }
    
    /// <inheritdoc/>
    public PartEntryState AddOrGetEntry(int logIndex, int partNodeId)
    {
        if (!_logs.TryGetValue(logIndex, out var log))
        {
            log = new LogState
            {
                LogIndex = logIndex
            };

            _logs[logIndex] = log;
        }

        if (!log.Entries.TryGetValue(partNodeId, out var entry))
        {
            entry = new PartEntryState
            {
                PartNodeId = partNodeId
            };

            log.Entries[partNodeId] = entry;
        }

        return entry;
    }

    /// <inheritdoc/>
    public bool TryGetEntry(int logIndex, int partNodeId, out PartEntryState? entry)
    {
        entry = null;

        return _logs.TryGetValue(logIndex, out var log) && log.Entries.TryGetValue(partNodeId, out entry);
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<PartEntryState> GetEntries(int logIndex)
    {
        if (!_logs.TryGetValue(logIndex, out var log))
            throw new KeyNotFoundException($"No entries found for logIndex {logIndex}");

        return log.Entries.Values.ToList().AsReadOnly();
    }


    /// <inheritdoc/>
    public bool TryGetEntries(int logIndex, out IReadOnlyCollection<PartEntryState>? entries)
    {
        entries = null;

        if (_logs.TryGetValue(logIndex, out var log))
        {
            entries = log.Entries.Values.ToList().AsReadOnly();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateTagCodeAsync(int logIndex, int partNodeId, string? tagCode, int partDefinitionId)
    {
        var entry = GetEntry(logIndex, partNodeId);
        entry.TagCode = tagCode;
        entry.SerializablePartId = null;

        if (string.IsNullOrWhiteSpace(tagCode))
            return false;

        var resolvedId = await _serializablePartService.TryResolveTagAsync(tagCode, partDefinitionId);

        if (resolvedId.HasValue)
        {
            entry.SerializablePartId = resolvedId.Value;
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void UpdateSerialNumber(int logIndex, int partNodeId, string? serialNumber)
    {
        var entry = GetEntry(logIndex, partNodeId);
        entry.SerialNumber = serialNumber;
    }
    
    /// <inheritdoc/>
    public void SetShouldProducePart(int logIndex, bool shouldProduce)
    {
        if (!_logs.TryGetValue(logIndex, out var log))
            throw new KeyNotFoundException($"Log index {logIndex} does not exist.");

        log.ShouldProducePart = shouldProduce;
    }
    
    /// <inheritdoc/>
    public bool ShouldCreateProducedPart(int logIndex)
    {
        if (!_logs.TryGetValue(logIndex, out var log))
            throw new KeyNotFoundException($"Log index {logIndex} does not exist.");

        return log.ShouldProducePart;
    }
    
    /// <inheritdoc/>
    public bool RemoveLog(int logIndex)
    {
        return _logs.Remove(logIndex);
    }

    /// <inheritdoc/>
    public bool HasLog(int logIndex) => _logs.ContainsKey(logIndex);
    
    /// <inheritdoc/>
    public int GetTotalPartsLogged()
    {
        return _logs.Values
            .SelectMany(log => log.Entries.Values)
            .Count(entry => entry.HasInput);
    }
    
    /// <inheritdoc/>
    public async Task<bool> HasUnresolvedTagsAsync(int? logIndexFilter = null, bool onlyWithInput = true)
    {
        var logs = _logs.Values.AsEnumerable();

        if (logIndexFilter.HasValue)
        {
            if (!_logs.TryGetValue(logIndexFilter.Value, out var log))
                throw new KeyNotFoundException($"Log index {logIndexFilter.Value} does not exist.");

            logs = [log];
        }

        foreach (var log in logs)
        {
            // Check entries
            foreach (var entry in log.Entries.Values)
            {
                if (!onlyWithInput || entry.HasInput)
                {
                    if (!string.IsNullOrWhiteSpace(entry.TagCode) && entry.SerializablePartId == null)
                        return true;
                }
            }

            // Check produced part if applicable
            // Check produced part if applicable
            if (log.ShouldProducePart)
            {
                var tagCode = log.ProducedPartTagCode;

                // Only check availability if there is actual input
                if (!string.IsNullOrWhiteSpace(tagCode) &&
                    !await _tagService.IsAvailableAsync(tagCode))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _logs.Clear();
    }

    /// <inheritdoc/>
    public void SetProducedPartSerialNumber(int logIndex, string? serialNumber)
    {
        var log = EnsureLogExists(logIndex);
        log.ProducedPartSerialNumber = serialNumber;
    }
    
    /// <inheritdoc/>
    public async Task SetProducedPartTagCodeAsync(int logIndex, string? tagCode, int partDefinitionId)
    {
        var log = EnsureLogExists(logIndex);
        log.ProducedPartTagCode = tagCode;
        log.ProducedPartSerializablePartId = null;

        if (string.IsNullOrWhiteSpace(tagCode))
            return;

        // Attempt to resolve tag to a serializable part
        var resolvedId = await _serializablePartService.TryResolveTagAsync(tagCode, partDefinitionId);
        if (resolvedId.HasValue)
            log.ProducedPartSerializablePartId = resolvedId.Value;
    }
    
    /// <summary>
    /// Ensures that a <see cref="LogState"/> exists for the given log index.
    /// If it does not exist, it is created.
    /// </summary>
    /// <param name="logIndex">The log index to ensure.</param>
    /// <returns>The existing or newly created <see cref="LogState"/>.</returns>
    private LogState EnsureLogExists(int logIndex)
    {
        if (!_logs.TryGetValue(logIndex, out var log))
        {
            log = new LogState
            {
                LogIndex = logIndex
            };

            _logs[logIndex] = log;
        }

        return log;
    }
    
    /// <inheritdoc/>
    public PartTraceabilitySnapshot CreateSnapshot(int logIndex)
    {
        var log = EnsureLogExists(logIndex);

        var snapshotEntries = log.Entries.Values
            .Select(e => new PartTraceabilitySnapshot.PartEntrySnapshot
            {
                PartNodeId = e.PartNodeId,
                SerialNumber = e.SerialNumber,
                TagCode = e.TagCode,
                SerializablePartId = e.SerializablePartId
            })
            .ToList();

        return new PartTraceabilitySnapshot
        {
            LogIndex = logIndex,
            ProducedPartSerialNumber = log.ProducedPartSerialNumber,
            ProducedPartTagCode = log.ProducedPartTagCode,
            ShouldProducePart = log.ShouldProducePart,
            Entries = snapshotEntries
        };
    }
    
    /// <inheritdoc/>
    public void LoadSnapshots(IEnumerable<PartTraceabilitySnapshot> snapshots)
    {
        if (snapshots == null)
            throw new ArgumentNullException(nameof(snapshots));

        _logs.Clear();

        foreach (var snapshot in snapshots)
        {
            var log = new LogState
            {
                LogIndex = snapshot.LogIndex,
                ShouldProducePart = snapshot.ShouldProducePart,
                ProducedPartSerialNumber = snapshot.ProducedPartSerialNumber,
                ProducedPartTagCode = snapshot.ProducedPartTagCode,
                ProducedPartSerializablePartId = null // will optionally resolve below
            };

            // Rehydrate entries
            foreach (var entrySnapshot in snapshot.Entries)
            {
                log.Entries[entrySnapshot.PartNodeId] = new PartEntryState
                {
                    PartNodeId = entrySnapshot.PartNodeId,
                    SerialNumber = entrySnapshot.SerialNumber,
                    TagCode = entrySnapshot.TagCode,
                    SerializablePartId = entrySnapshot.SerializablePartId
                };
            }

            _logs[snapshot.LogIndex] = log;
        }
        
        // **Notify subscribers that state has changed**
        OnStateChanged?.Invoke();
    }
    
    /// <inheritdoc/>
    public string Dump(int? logIndexFilter = null, bool onlyWithInput = false)
    {
        var sb = new StringBuilder();

        sb.AppendLine("===== PartTraceabilityStateService Dump =====");

        if (_logs.Count == 0)
        {
            sb.AppendLine("No logs present.");
            return sb.ToString();
        }

        foreach (var (logIndex, log) in _logs)
        {
            if (logIndexFilter.HasValue && logIndex != logIndexFilter.Value)
                continue;

            sb.AppendLine($"\nLogIndex: {logIndex}");
            sb.AppendLine($"  ShouldProducePart: {log.ShouldProducePart}");
            sb.AppendLine($"  ProducedPartSerialNumber: {log.ProducedPartSerialNumber ?? "[null]"}");

            if (log.Entries.Count == 0)
            {
                sb.AppendLine("  No entries.");
                continue;
            }

            foreach (var (partNodeId, entry) in log.Entries)
            {
                if (onlyWithInput && !entry.HasInput)
                    continue;

                var marker = entry.HasInput ? "[X]" : "[ ]";

                sb.AppendLine($"  {marker} PartNodeId: {partNodeId}");
                sb.AppendLine($"    SerialNumber: {entry.SerialNumber ?? "[null]"}");
                sb.AppendLine($"    TagCode: {entry.TagCode ?? "[null]"}");
                sb.AppendLine($"    SerializablePartId: {entry.SerializablePartId?.ToString() ?? "[null]"}");
            }
        }

        sb.AppendLine("\n===== End Dump =====");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public void DumpToConsole(int? logIndexFilter = null, bool onlyWithInput = false)
    {
        Console.WriteLine(Dump(logIndexFilter, onlyWithInput));
    }
}