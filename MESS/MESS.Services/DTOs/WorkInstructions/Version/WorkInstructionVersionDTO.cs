namespace MESS.Services.DTOs.WorkInstructions.Version
{
    /// <summary>
    /// Represents a single persisted version of a work instruction
    /// for display in version history views.
    /// </summary>
    /// <remarks>
    /// Each version corresponds to a distinct database row.
    /// This DTO is intentionally lightweight and excludes
    /// step content, product associations, and editing metadata.
    /// </remarks>
    public sealed record WorkInstructionVersionDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of this specific version row.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Gets or sets the identifier shared by all versions
        /// of the same logical work instruction.
        /// </summary>
        public int RootInstructionId { get; init; }

        /// <summary>
        /// Gets the user-defined version label of the work instruction.
        /// </summary>
        /// <remarks>
        /// This value is optional and is not used for system ordering.
        /// Version history ordering is determined by <see cref="LastModifiedOn"/>.
        /// </remarks>
        public string? Version { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the title of the work instruction
        /// at the time this version was saved.
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Gets the timestamp indicating when this version
        /// was last modified.
        /// </summary>
        /// <remarks>
        /// Stored as a <see cref="DateTimeOffset"/> to preserve
        /// both the absolute point in time and the originating
        /// time zone offset.
        /// 
        /// This is preferred over <see cref="DateTime"/> for
        /// auditability, distributed systems, and multi-time-zone
        /// environments.
        /// </remarks>
        public DateTimeOffset LastModifiedOn { get; init; }

        /// <summary>
        /// Gets or sets the identifier or display name of the user
        /// who last modified this version.
        /// </summary>
        public string? LastModifiedBy { get; init; }

        /// <summary>
        /// True when at least one production log references this specific work instruction version.
        /// Used to gate deletion: Technicians cannot delete a version with associated logs.
        /// </summary>
        public bool HasProductionLogs { get; init; }
    }
}