namespace MESS.Services.CRUD.WorkInstructions;

/// <summary>
/// Thrown when a save operation would create a duplicate (Title, Version) combination.
/// The database unique index is the last-resort guard; this exception allows the UI to
/// show a user-friendly error before hitting the constraint.
/// </summary>
public sealed class WorkInstructionNotUniqueException(string title, string? version)
    : Exception($"A work instruction named \"{title}\" with version \"{version ?? "(no version)"}\" already exists. Choose a different title or version.");
