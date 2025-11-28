using DevExpress.Xpo;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    /// <summary>
    /// Persistent memory record for a node.
    /// Stores the rolling summary that represents the node's conversational context.
    /// </summary>
    [Persistent("NodeMemories")]
    public class NodeMemory : XPObject
    {
        private string? _nodeId;
        private string? _summary;
        private DateTime _updatedAt = DateTime.UtcNow;
        private DateTime _createdAt = DateTime.UtcNow;

        public NodeMemory(Session session) : base(session) { }
        public NodeMemory() : base(Session.DefaultSession) { }

        /// <summary>
        /// The unique identifier of the node this memory belongs to.
        /// </summary>
        [Size(255)]
        [Indexed(Unique = true)]
        public string? NodeId
        {
            get => _nodeId;
            set => SetPropertyValue(nameof(NodeId), ref _nodeId, value);
        }

        /// <summary>
        /// The rolling summary of the node's conversation history.
        /// Contains compressed context including user goals, constraints, preferences,
        /// and AI decisions/commitments.
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? Summary
        {
            get => _summary;
            set => SetPropertyValue(nameof(Summary), ref _summary, value);
        }

        /// <summary>
        /// Timestamp of when this memory was last updated.
        /// </summary>
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetPropertyValue(nameof(UpdatedAt), ref _updatedAt, value);
        }

        /// <summary>
        /// Timestamp of when this memory was created.
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetPropertyValue(nameof(CreatedAt), ref _createdAt, value);
        }
    }
}
