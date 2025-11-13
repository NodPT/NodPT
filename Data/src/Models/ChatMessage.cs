using DevExpress.Xpo;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class ChatMessage : XPObject
    {
        private string? _sender;
        private string? _message;
        private DateTime _timestamp = DateTime.UtcNow;
        private bool _markedAsSolution;
        private Node? _node;
        private bool _liked;
        private bool _disliked;
        private User? _user;

        public ChatMessage(Session session) : base(session) { }
        public ChatMessage() : base(Session.DefaultSession) { }

        public string? Sender
        {
            get => _sender;
            set => SetPropertyValue(nameof(Sender), ref _sender, value);
        }

        [Size(SizeAttribute.Unlimited)]
        public string? Message
        {
            get => _message;
            set => SetPropertyValue(nameof(Message), ref _message, value);
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetPropertyValue(nameof(Timestamp), ref _timestamp, value);
        }

        public bool MarkedAsSolution
        {
            get => _markedAsSolution;
            set => SetPropertyValue(nameof(MarkedAsSolution), ref _markedAsSolution, value);
        }

        [Association("Node-ChatMessages")]
        [JsonIgnore]
        public Node? Node
        {
            get => _node;
            set => SetPropertyValue(nameof(Node), ref _node, value);
        }

        public bool Liked
        {
            get => _liked;
            set => SetPropertyValue(nameof(Liked), ref _liked, value);
        }

        public bool Disliked
        {
            get => _disliked;
            set => SetPropertyValue(nameof(Disliked), ref _disliked, value);
        }

        [Association("User-ChatMessages")]
        [JsonIgnore]
        public User? User
        {
            get => _user;
            set => SetPropertyValue(nameof(User), ref _user, value);
        }

        [Association("ChatMessage-ChatResponses")]
        [JsonIgnore]
        public XPCollection<ChatResponse> ChatResponses
        {
            get => GetCollection<ChatResponse>(nameof(ChatResponses));
        }
    }
}
