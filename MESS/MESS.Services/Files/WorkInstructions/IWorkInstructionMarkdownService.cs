using MESS.Services.DTOs.WorkInstructions.File;

namespace MESS.Services.Files.WorkInstructions;

/// <summary>
/// Defines a service for parsing and serializing work instruction data
/// to and from a Markdown-based file format.
/// 
/// This service is responsible for converting between raw Markdown content
/// and <see cref="WorkInstructionFileDTO"/> representations used within the application.
/// 
/// Implementations should adhere to the defined Markdown structure and conventions
/// for work instructions, including front matter metadata and node formatting.
/// </summary>
public interface IWorkInstructionMarkdownService
{
    /// <summary>
    /// Parses a Markdown string into a <see cref="WorkInstructionFileDTO"/>.
    /// 
    /// The Markdown is expected to follow the defined work instruction format,
    /// including optional YAML front matter and structured step/part nodes.
    /// </summary>
    /// <param name="markdown">
    /// The raw Markdown content representing a work instruction.
    /// </param>
    /// <returns>
    /// A <see cref="WorkInstructionFileDTO"/> containing the parsed data.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown when the Markdown content is invalid or does not conform
    /// to the expected structure.
    /// </exception>
    WorkInstructionFileDTO Parse(string markdown);

    /// <summary>
    /// Serializes a <see cref="WorkInstructionFileDTO"/> into a Markdown string.
    /// 
    /// The output will follow the standardized work instruction format,
    /// including YAML front matter and properly structured nodes.
    /// </summary>
    /// <param name="dto">
    /// The work instruction data to serialize.
    /// </param>
    /// <returns>
    /// A Markdown string representation of the provided work instruction.
    /// </returns>
    string Serialize(WorkInstructionFileDTO dto);
}