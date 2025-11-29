using DevExpress.Xpo;
using NodPT.Data.DTOs;

namespace NodPT.Data.Interfaces
{

    /// <summary>
    /// Central memory coordinator service.
    /// Manages rolling summaries and short-term message history for nodes.
    /// </summary>
    public interface IMemoryService
    {
        /// <summary>
        /// Load the current summary for a node.
        /// Checks Redis first, then falls back to database, then initializes empty.
        /// </summary>
        /// <param name="nodeId">The node identifier</param>
        /// <param name="unitOfWork">XPO UnitOfWork for database access</param>
        /// <returns>The current summary text (may be empty string)</returns>
        Task<string> LoadSummaryAsync(string nodeId, UnitOfWork unitOfWork);

        /// <summary>
        /// Perform rolling summarization after a new message.
        /// Updates both Redis and database with the new summary.
        /// This is a blocking operation.
        /// </summary>
        /// <param name="nodeId">The node identifier</param>
        /// <param name="newMessageContent">The content of the new message</param>
        /// <param name="role">The role: "user" or "ai_assistant"</param>
        /// <param name="unitOfWork">XPO UnitOfWork for database access</param>
        /// <returns>The new summary text</returns>
        Task<string> RollingSummarizeAsync(string nodeId, string newMessageContent, string role, UnitOfWork unitOfWork);

        /// <summary>
        /// Queue rolling summarization to run in the background (non-blocking).
        /// This allows the chat flow to continue without waiting for summarization.
        /// </summary>
        /// <param name="nodeId">The node identifier</param>
        /// <param name="newMessageContent">The content of the new message</param>
        /// <param name="role">The role: "user" or "ai_assistant"</param>
        void QueueSummarization(string nodeId, string newMessageContent, string role);

        /// <summary>
        /// Add a message to the short-term history in Redis.
        /// Automatically trims to the configured limit.
        /// </summary>
        /// <param name="nodeId">The node identifier</param>
        /// <param name="message">The message to add</param>
        Task AddToHistoryAsync(string nodeId, HistoryMessage message);

        /// <summary>
        /// Get the short-term message history for a node.
        /// </summary>
        /// <param name="nodeId">The node identifier</param>
        /// <returns>List of recent messages</returns>
        Task<List<HistoryMessage>> GetHistoryAsync(string nodeId);

        /// <summary>
        /// Clear all memory for a node (both summary and history).
        /// </summary>
        /// <param name="nodeId">The node identifier</param>
        /// <param name="unitOfWork">XPO UnitOfWork for database access</param>
        Task ClearMemoryAsync(string nodeId, UnitOfWork unitOfWork);
    }
}
