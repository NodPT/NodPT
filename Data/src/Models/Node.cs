using DevExpress.Xpo;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class Node : XPLiteObject
    {
        private string? _id;
        private string? _name;
        private NodeType _nodeType;
        private string? _properties;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;
        private string? _status;
        private Node? _parent;
        private Project? _project;
        private Template? _template;
        private MessageTypeEnum _messageType;

        public Node(Session session) : base(session) { }
        public Node() : base(Session.DefaultSession) { }

        [Key]
        public string? Id
        {
            get => _id;
            set => SetPropertyValue(nameof(Id), ref _id, value);
        }

        public string? Name
        {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        public NodeType NodeType
        {
            get => _nodeType;
            set => SetPropertyValue(nameof(NodeType), ref _nodeType, value);
        }

        [Size(SizeAttribute.Unlimited)]
        public string? Properties
        {
            get => _properties;
            set => SetPropertyValue(nameof(Properties), ref _properties, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetPropertyValue(nameof(CreatedAt), ref _createdAt, value);
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetPropertyValue(nameof(UpdatedAt), ref _updatedAt, value);
        }

        public string? Status
        {
            get => _status;
            set => SetPropertyValue(nameof(Status), ref _status, value);
        }

        [Association("Node-ChatMessages")]
        [JsonIgnore]
        public XPCollection<ChatMessage> ChatMessages => GetCollection<ChatMessage>(nameof(ChatMessages));

        [Association("ParentNode-ChildNodes")]
        [JsonIgnore]
        public Node? Parent
        {
            get => _parent;
            set => SetPropertyValue(nameof(Parent), ref _parent, value);
        }

        [Association("ParentNode-ChildNodes")]
        [JsonIgnore]
        public XPCollection<Node> Children => GetCollection<Node>(nameof(Children));

        [Association("Project-Nodes")]
        [JsonIgnore]
        public Project? Project
        {
            get => _project;
            set => SetPropertyValue(nameof(Project), ref _project, value);
        }

        /// <summary>
        /// Many-to-one relationship: Node can belong to a Template (nullable)
        /// </summary>
        [Association("Template-Nodes")]
        [JsonIgnore]
        public Template? Template
        {
            get => _template;
            set => SetPropertyValue(nameof(Template), ref _template, value);
        }

        /// <summary>
        /// Type of message: Discussion or Decision
        /// </summary>
        public MessageTypeEnum MessageType
        {
            get => _messageType;
            set => SetPropertyValue(nameof(MessageType), ref _messageType, value);
        }

        // Helper property to work with Properties as Dictionary
        [Browsable(false)]
        public Dictionary<string, string> PropertiesDictionary
        {
            get
            {
                if (string.IsNullOrEmpty(Properties))
                    return new Dictionary<string, string>();

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Properties)
                           ?? new Dictionary<string, string>();
                }
                catch
                {
                    return new Dictionary<string, string>();
                }
            }
            set
            {
                Properties = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }

        /// <summary>
        /// Returns the AIModel from Project.Template that matches this Node's MessageType and NodeType.
        /// If not found, creates a default AIModel and adds it to the Template's AIModels collection.
        /// </summary>
        public AIModel? GetMatchingAIModel()
        {
            if (Project?.Template == null) return null;

            // Try to find existing matching AIModel
            var matchingModel = Project.Template.AIModels
                .FirstOrDefault(am => am.MessageType == MessageType && am.NodeType == NodeType && am.IsActive);

            // If found, return it
            if (matchingModel != null)
                return matchingModel;

            // Create default AIModel if not found
            var defaultModel = new AIModel(Session)
            {
                Name = $"Default {NodeType} {MessageType}",
                ModelIdentifier = "llama3.2:3b",
                MessageType = MessageType,
                NodeType = NodeType,
                Description = $"Default AI model for {NodeType} type with {MessageType} message type",
                IsActive = true,
                Template = Project.Template,
                EndpointAddress = null, // Will use system default
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add to template's collection
            Project.Template.AIModels.Add(defaultModel);
            
            // Save the new model
            defaultModel.Save();

            return defaultModel;
        }

        /// <summary>
        /// Readonly property that returns the list of Prompts from Project.Template that match this Node's MessageType and NodeType
        /// </summary>
        public List<Prompt> GetMatchingPrompts()
        {
            if (Project?.Template == null) return new List<Prompt>();

            return Project.Template.Prompts
                .Where(p => p.MessageType == MessageType && p.NodeType == NodeType)
                .ToList();
        }
    }
}
