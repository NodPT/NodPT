namespace NodPT.Data.DTOs
{

    /// <summary>
    /// Configuration options for the memory service.
    /// </summary>
    public class MemoryOptions
    {
        /// <summary>
        /// Maximum number of recent messages to keep in Redis history.
        /// </summary>
        public int HistoryLimit { get; set; } = 3;

        /// <summary>
        /// Redis key prefix for storing node summaries.
        /// Keys will be formatted as: {prefix}:{nodeId}
        /// </summary>
        public string SummaryKeyPrefix { get; set; } = "summary";

        /// <summary>
        /// Redis key prefix for storing node message history.
        /// Keys will be formatted as: {prefix}:{nodeId}
        /// </summary>
        public string HistoryKeyPrefix { get; set; } = "history";
    }
}
