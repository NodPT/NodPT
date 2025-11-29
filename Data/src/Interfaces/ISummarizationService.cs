namespace NodPT.Data.Interfaces
{

    /// <summary>
    /// Service for calling Ollama summarizer endpoint to perform rolling summarization.
    /// This service is responsible ONLY for the actual summarization request.
    /// It does not touch Redis or the database.
    /// </summary>
    public interface ISummarizationService
    {
        /// <summary>
        /// Summarize an old summary with a new message to produce an updated summary.
        /// </summary>
        /// <param name="oldSummary">The existing summary text</param>
        /// <param name="newMessageContent">The new message to integrate</param>
        /// <param name="role">The role of the message sender: "user" or "ai_assistant"</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The new merged summary text</returns>
        Task<string> SummarizeAsync(string oldSummary, string newMessageContent, string role, CancellationToken cancellationToken = default);
    }
}
