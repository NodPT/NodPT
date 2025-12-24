using DevExpress.Xpo;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class AIModel : XPObject
    {
        private string? _name;
        private string? _modelIdentifier;
        private MessageTypeEnum _messageType;
        private NodeType _nodeType;
        private string? _description;
        private bool _isActive = true;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;
        private Template? _template;

        // Ollama API endpoint and parameters
        private string? _endpointAddress;
        private double? _temperature;
        private int? _numPredict;
        private int? _topK;
        private double? _topP;
        private int? _seed;
        private int? _numCtx;
        private int? _numGpu;
        private int? _numThread;
        private double? _repeatPenalty;
        private string? _stop;

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
        /// Node type: Director, Manager, Inspector, Worker, etc.
        /// </summary>
        public NodeType NodeType
        {
            get => _nodeType;
            set => SetPropertyValue(nameof(NodeType), ref _nodeType, value);
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
        /// Ollama API endpoint address (e.g., "http://localhost:11434/api/chat")
        /// </summary>
        [Size(500)]
        public string? EndpointAddress
        {
            get => _endpointAddress;
            set => SetPropertyValue(nameof(EndpointAddress), ref _endpointAddress, value);
        }

        /// <summary>
        /// Temperature for sampling (0.0-2.0). Higher values make output more random.
        /// </summary>
        public double? Temperature
        {
            get => _temperature;
            set => SetPropertyValue(nameof(Temperature), ref _temperature, value);
        }

        /// <summary>
        /// Maximum number of tokens to predict.
        /// </summary>
        public int? NumPredict
        {
            get => _numPredict;
            set => SetPropertyValue(nameof(NumPredict), ref _numPredict, value);
        }

        /// <summary>
        /// Top-K sampling: limits token selection to K most likely tokens.
        /// </summary>
        public int? TopK
        {
            get => _topK;
            set => SetPropertyValue(nameof(TopK), ref _topK, value);
        }

        /// <summary>
        /// Top-P (nucleus) sampling: limits token selection to cumulative probability P.
        /// </summary>
        public double? TopP
        {
            get => _topP;
            set => SetPropertyValue(nameof(TopP), ref _topP, value);
        }

        /// <summary>
        /// Random seed for reproducibility. Set to 0 for random.
        /// </summary>
        public int? Seed
        {
            get => _seed;
            set => SetPropertyValue(nameof(Seed), ref _seed, value);
        }

        /// <summary>
        /// Context window size (number of tokens).
        /// </summary>
        public int? NumCtx
        {
            get => _numCtx;
            set => SetPropertyValue(nameof(NumCtx), ref _numCtx, value);
        }

        /// <summary>
        /// Number of GPU layers to use.
        /// </summary>
        public int? NumGpu
        {
            get => _numGpu;
            set => SetPropertyValue(nameof(NumGpu), ref _numGpu, value);
        }

        /// <summary>
        /// Number of threads to use for computation.
        /// </summary>
        public int? NumThread
        {
            get => _numThread;
            set => SetPropertyValue(nameof(NumThread), ref _numThread, value);
        }

        /// <summary>
        /// Repeat penalty for reducing repetition (1.0 = no penalty).
        /// </summary>
        public double? RepeatPenalty
        {
            get => _repeatPenalty;
            set => SetPropertyValue(nameof(RepeatPenalty), ref _repeatPenalty, value);
        }

        /// <summary>
        /// Stop sequences (comma-separated) that signal the model to stop generating.
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? Stop
        {
            get => _stop;
            set => SetPropertyValue(nameof(Stop), ref _stop, value);
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
    }
}
