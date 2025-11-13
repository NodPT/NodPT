using DevExpress.Xpo;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class AIModel : XPObject
    {
        private string? _name;
        private string? _modelIdentifier;
        private MessageTypeEnum _messageType;
        private LevelEnum _level;
        private string? _description;
        private bool _isActive = true;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;
        private Template? _template;

        public AIModel(Session session) : base(session) { }
        public AIModel() : base(Session.DefaultSession) { }

        /// <summary>
        /// Name of the AI model configuration
        /// </summary>
        [Size(255)]
        public string? Name
        {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        /// <summary>
        /// Model identifier (e.g., "gpt-4", "claude-3-opus", etc.)
        /// </summary>
        [Size(255)]
        public string? ModelIdentifier
        {
            get => _modelIdentifier;
            set => SetPropertyValue(nameof(ModelIdentifier), ref _modelIdentifier, value);
        }

        /// <summary>
        /// Type of the AI model: Discussion or Decision
        /// </summary>
        public MessageTypeEnum MessageType
        {
            get => _messageType;
            set => SetPropertyValue(nameof(MessageType), ref _messageType, value);
        }

        /// <summary>
        /// Level of the AI model: Brain, Manager, Inspector, or Worker
        /// </summary>
        public LevelEnum Level
        {
            get => _level;
            set => SetPropertyValue(nameof(Level), ref _level, value);
        }

        /// <summary>
        /// Description of the AI model configuration
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? Description
        {
            get => _description;
            set => SetPropertyValue(nameof(Description), ref _description, value);
        }

        /// <summary>
        /// Whether the AI model is active and available for use
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetPropertyValue(nameof(IsActive), ref _isActive, value);
        }

        /// <summary>
        /// When the AI model was created
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetPropertyValue(nameof(CreatedAt), ref _createdAt, value);
        }

        /// <summary>
        /// When the AI model was last updated
        /// </summary>
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetPropertyValue(nameof(UpdatedAt), ref _updatedAt, value);
        }

        /// <summary>
        /// Many-to-one relationship: AIModel belongs to a Template
        /// </summary>
        [Association("Template-AIModels")]
        [JsonIgnore]
        public Template? Template
        {
            get => _template;
            set => SetPropertyValue(nameof(Template), ref _template, value);
        }

        /// <summary>
        /// One-to-many relationship: AIModel can have many Nodes
        /// </summary>
        [Association("AIModel-Nodes")]
        [JsonIgnore]
        public XPCollection<Node> Nodes => GetCollection<Node>(nameof(Nodes));
    }
}
