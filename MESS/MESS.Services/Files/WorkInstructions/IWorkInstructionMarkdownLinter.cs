namespace MESS.Services.Files.WorkInstructions;

/// <summary>
/// Defines a service responsible for validating work instruction Markdown content
/// against the expected MESS Markdown format rules.
/// 
/// This linter performs structural and syntactic validation only. It does not
/// modify the input content or perform parsing into domain models.
/// </summary>
public interface IWorkInstructionMarkdownLinter
{
    /// <summary>
    /// Validates the provided Markdown content representing a work instruction.
    /// 
    /// The linter checks for structural correctness such as required front matter,
    /// valid step and part node formatting, and adherence to expected Markdown
    /// conventions defined by the MESS specification.
    /// </summary>
    /// <param name="markdown">
    /// The raw Markdown content of a work instruction to validate.
    /// </param>
    /// <returns>
    /// A list of validation error messages. If the list is empty, the Markdown
    /// is considered valid according to the current linter rules.
    /// </returns>
    List<string> Lint(string markdown);
}