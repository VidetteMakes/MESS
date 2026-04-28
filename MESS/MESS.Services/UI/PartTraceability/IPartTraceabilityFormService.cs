namespace MESS.Services.UI.PartTraceability
{
    /// <summary>
    /// A service used for accessing and mutating the current UI state of part traceability operations for a given batch
    /// of production logs.
    /// </summary>
    public interface IPartTraceabilityFormService
    {
        /// <summary>
        /// Called whenever snapshots are loaded or state changes.
        /// Components can subscribe to this to trigger a UI refresh.
        /// </summary>
        public Action? OnStateChanged { get; set; }
        
        /// <summary>
        /// Initializes the state service with the specified production logs and node blocks.
        /// </summary>
        /// <param name="logIndexes">The log indexes for each production log.</param>
        /// <param name="nodeBlocks">
        /// The ordered list of node blocks (each block is a list of part nodes) from the work instruction.
        /// </param>
        /// <remarks>
        /// Each log index will have a corresponding list of <see cref="PartEntryGroupState"/>
        /// one per node block. Each group will contain entries for the part nodes in that block.
        /// Existing state is cleared.
        /// </remarks>
        void Initialize(IEnumerable<int> logIndexes, IEnumerable<List<int>> nodeBlocks);
        
        /// <summary>
        /// Retrieves a specific <see cref="PartEntryState"/> by log index and part node identifier.
        /// </summary>
        /// <param name="logIndex">The production log index.</param>
        /// <param name="partNodeId">The unique part node identifier within the log.</param>
        /// <returns>The corresponding <see cref="PartEntryState"/>.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the log or entry does not exist.
        /// </exception>
        PartEntryState GetEntry(int logIndex, int partNodeId);
        
        /// <summary>
        /// Ensures a <see cref="PartEntryState"/> exists for the specified log index and part node.
        /// If no entry exists, a new one is created and stored.
        /// </summary>
        /// <param name="logIndex">The production log index.</param>
        /// <param name="partNodeId">The unique identifier of the part node.</param>
        /// <returns>The existing or newly created <see cref="PartEntryState"/>.</returns>
        PartEntryState AddOrGetEntry(int logIndex, int partNodeId);

        /// <summary>
        /// Attempts to retrieve a specific <see cref="PartEntryState"/> by log index and part node identifier.
        /// </summary>
        /// <param name="logIndex">The production log index.</param>
        /// <param name="partNodeId">The unique part node identifier within the log.</param>
        /// <param name="entry">The resulting entry if found; otherwise null.</param>
        /// <returns>True if the entry exists; otherwise false.</returns>
        bool TryGetEntry(int logIndex, int partNodeId, out PartEntryState? entry);
        
        /// <summary>
        /// Retrieves all part entry states for a specific production log.
        /// </summary>
        /// <param name="logIndex">The zero-based UI index representing the production log.</param>
        /// <returns>
        /// A read-only collection of <see cref="PartEntryState"/> instances for the specified log.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if no entries exist for the given <paramref name="logIndex"/>.
        /// </exception>
        IReadOnlyCollection<PartEntryState> GetEntries(int logIndex);
        
        /// <summary>
        /// Attempts to retrieve all part entry states for a specific production log.
        /// </summary>
        /// <param name="logIndex">The zero-based UI index representing the production log.</param>
        /// <param name="entries">
        /// When this method returns, contains a read-only collection of <see cref="PartEntryState"/> instances
        /// if the log exists; otherwise, <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if entries exist for the specified log index; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method provides safe, exception-free access to part entry states.
        /// Use it when you do not want to throw an exception if the log index does not exist.
        /// </remarks>
        bool TryGetEntries(int logIndex, out IReadOnlyCollection<PartEntryState>? entries);

        /// <summary>
        /// Updates the tag code for a specific part entry and attempts to resolve it to a serializable part.
        /// </summary>
        /// <param name="logIndex">The production log index.</param>
        /// <param name="partNodeId">The unique part node identifier within the log.</param>
        /// <param name="tagCode">The tag code entered by the user.</param>
        /// <param name="partDefinitionId">The part definition ID to use for resolution/validation.</param>
        /// <returns>
        /// <c>true</c> if the tag code was successfully resolved to a serializable part; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method updates the in-memory <see cref="PartEntryState.TagCode"/> and optionally
        /// sets <see cref="PartEntryState.SerializablePartId"/> if resolution succeeds.
        /// The caller can use the return value to provide UI feedback to the user.
        /// </remarks>
        Task<bool> UpdateTagCodeAsync(int logIndex, int partNodeId, string? tagCode, int partDefinitionId);

        /// <summary>
        /// Updates the serial number for a specific part entry.
        /// </summary>
        /// <param name="logIndex">The production log index.</param>
        /// <param name="partNodeId">The unique part node identifier within the log.</param>
        /// <param name="serialNumber">The serial number entered by the user.</param>
        void UpdateSerialNumber(int logIndex, int partNodeId, string? serialNumber);
        
        /// <summary>
        /// Sets whether the specified log should result in a produced part.
        /// </summary>
        /// <param name="logIndex">The log index to update.</param>
        /// <param name="shouldProduce">
        /// <c>true</c> if a produced part should be created; otherwise, <c>false</c>.
        /// </param>
        void SetShouldProducePart(int logIndex, bool shouldProduce);

        /// <summary>
        /// Determines whether the specified log should result in a produced part.
        /// </summary>
        /// <param name="logIndex">The log index to evaluate.</param>
        /// <returns>
        /// <c>true</c> if a produced part should be created; otherwise, <c>false</c>.
        /// </returns>
        bool ShouldCreateProducedPart(int logIndex);
        
        /// <summary>
        /// Removes all state associated with a specific production log index.
        /// </summary>
        /// <param name="logIndex">The production log index.</param>
        /// <returns>
        /// <c>true</c> if the groups were found and removed; otherwise <c>false</c>.
        /// </returns>
        bool RemoveLog(int logIndex);
        
        /// <summary>
        /// Determines whether state exists for the specified production log index.
        /// </summary>
        bool HasLog(int logIndex);

        /// <summary>
        /// Gets the total number of part entries that have user input across all logs.
        /// </summary>
        /// <returns>
        /// The count of <see cref="PartEntryState"/> instances that contain either a serial number or tag code.
        /// </returns>
        int GetTotalPartsLogged();
        
        /// <summary>
        /// Determines whether any part entry contains a tag code that could not be resolved
        /// to a corresponding serializable part.
        /// </summary>
        /// <param name="logIndexFilter">
        /// Optional log index to restrict the check to a specific log.
        /// If <c>null</c>, all logs are evaluated.
        /// </param>
        /// <param name="onlyWithInput">
        /// If <c>true</c>, only entries that contain user input are considered.
        /// If <c>false</c>, all entries are evaluated.
        /// </param>
        /// <returns>
        /// <c>true</c> if at least one entry has a non-empty tag code that failed to resolve;
        /// otherwise, <c>false</c>.
        /// </returns>
        Task<bool> HasUnresolvedTagsAsync(int? logIndexFilter = null, bool onlyWithInput = true);

        /// <summary>
        /// Clears all state managed by the service.
        /// </summary>
        void Clear();

        /// <summary>
        /// Sets or clears the produced part serial number for a specific production log.
        /// </summary>
        /// <param name="logIndex">The production log index.</param>
        /// <param name="serialNumber">The serial number to set, or null to clear.</param>
        void SetProducedPartSerialNumber(int logIndex, string? serialNumber);

        /// <summary>
        /// Sets the tag code for the produced part in a specific log.
        /// Clears any previously resolved serializable part ID for the produced part.
        /// </summary>
        /// <param name="logIndex">The UI log index.</param>
        /// <param name="tagCode">The tag code to assign to the produced part.</param>
        /// <param name="partDefinitionId">The part definition ID to use for resolving the tag code to a serializable part.</param>
        Task SetProducedPartTagCodeAsync(int logIndex, string? tagCode, int partDefinitionId);

        /// <summary>
        /// Creates an immutable snapshot of the current part traceability state
        /// for a single production log identified by its UI log index.
        /// </summary>
        /// <param name="logIndex">
        /// The zero-based UI index representing the production log whose state should be captured.
        /// This value is managed by the UI layer and is not a database identifier.
        /// </param>
        /// <returns>
        /// A <see cref="PartTraceabilitySnapshot"/> containing the flattened part entries
        /// and produced part serial number for the specified log index.
        /// </returns>
        /// <remarks>
        /// This method captures the current in-memory state of part traceability inputs,
        /// including serial numbers, tag codes, and any resolved serializable part IDs.
        ///
        /// The returned snapshot is intended for use by higher-level application services,
        /// which may transform it into persistence DTOs (e.g., PartTraceabilityOperation)
        /// by supplying a corresponding ProductionLogId.
        ///
        /// This method does not perform any database access and does not require that
        /// the production log has been persisted.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no part traceability state exists for the specified <paramref name="logIndex"/>.
        /// </exception>
        PartTraceabilitySnapshot CreateSnapshot(int logIndex);
        
        /// <summary>
        /// Loads a collection of <see cref="PartTraceabilitySnapshot"/> instances into the state service,
        /// replacing any existing log state.
        /// </summary>
        /// <param name="snapshots">
        /// The snapshots to load. Each snapshot represents the full state of a log, including its entries
        /// and produced part data.
        /// </param>
        void LoadSnapshots(IEnumerable<PartTraceabilitySnapshot> snapshots);

        /// <summary>
        /// Dumps the current state as a formatted string.
        /// </summary>
        /// <param name="logIndexFilter">Optional log index to filter.</param>
        /// <param name="onlyWithInput">If true, only entries with input are included.</param>
        string Dump(int? logIndexFilter = null, bool onlyWithInput = false);

        /// <summary>
        /// Writes the current state to the console.
        /// </summary>
        void DumpToConsole(int? logIndexFilter = null, bool onlyWithInput = false);
    }
}