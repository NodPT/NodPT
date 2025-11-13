using DevExpress.Xpo;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class Prompt : XPObject
    {
        private string? _content;
        private MessageTypeEnum _messageType;
        private LevelEnum _level;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;
        private Template? _template;

        public Prompt(Session session) : base(session) { }
        public Prompt() : base(Session.DefaultSession) { }

        /// <summary>
        /// The content that will be injected into the chat message to allow AI to follow instructions
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? Content
        {
            get => _content;
            set => SetPropertyValue(nameof(Content), ref _content, value);
        }

        /// <summary>
        /// Type of the prompt: Discussion or Decision
        /// </summary>
        public MessageTypeEnum MessageType
        {
            get => _messageType;
            set => SetPropertyValue(nameof(MessageType), ref _messageType, value);
        }

        /// <summary>
        /// Level of the prompt: Brain, Manager, Inspector, or Worker
        /// </summary>
        public LevelEnum Level
        {
            get => _level;
            set => SetPropertyValue(nameof(Level), ref _level, value);
        }

        /// <summary>
        /// When the prompt was created
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetPropertyValue(nameof(CreatedAt), ref _createdAt, value);
        }

        /// <summary>
        /// When the prompt was last updated
        /// </summary>
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetPropertyValue(nameof(UpdatedAt), ref _updatedAt, value);
        }

        /// <summary>
        /// Many-to-one relationship: Prompt belongs to a Template
        /// </summary>
        [Association("Template-Prompts")]
        [JsonIgnore]
        public Template? Template
        {
            get => _template;
            set => SetPropertyValue(nameof(Template), ref _template, value);
        }
    }
}
