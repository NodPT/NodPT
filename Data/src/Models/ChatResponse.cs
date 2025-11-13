using DevExpress.Xpo;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class ChatResponse : XPObject
    {
        private ChatMessage? _chatMessage;
        private string? _action; // "like", "dislike", "regenerate"
        private DateTime _timestamp = DateTime.UtcNow;
        private User? _user;

        public ChatResponse(Session session) : base(session) { }
        public ChatResponse() : base(Session.DefaultSession) { }

        [Association("ChatMessage-ChatResponses")]
        [JsonIgnore]
        public ChatMessage? ChatMessage
        {
            get => _chatMessage;
            set => SetPropertyValue(nameof(ChatMessage), ref _chatMessage, value);
        }

        public string? Action
        {
            get => _action;
            set => SetPropertyValue(nameof(Action), ref _action, value);
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetPropertyValue(nameof(Timestamp), ref _timestamp, value);
        }

        [Association("User-ChatResponses")]
        [JsonIgnore]
        public User? User
        {
            get => _user;
            set => SetPropertyValue(nameof(User), ref _user, value);
        }
    }
}