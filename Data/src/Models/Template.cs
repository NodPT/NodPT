using DevExpress.Xpo;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class Template : XPObject
    {
        private string? _name;
        private string? _description;
        private string? _category;
        private string? _version;
        private bool _isActive = true;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;

        public Template(Session session) : base(session) { }
        public Template() : base(Session.DefaultSession) { }

        /// <summary>
        /// Template name
        /// </summary>
        [Size(255)]
        [Indexed]
        public string? Name
        {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        /// <summary>
        /// Template description
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? Description
        {
            get => _description;
            set => SetPropertyValue(nameof(Description), ref _description, value);
        }

        /// <summary>
        /// Template category for organization
        /// </summary>
        [Size(100)]
        [Indexed]
        public string? Category
        {
            get => _category;
            set => SetPropertyValue(nameof(Category), ref _category, value);
        }

        /// <summary>
        /// Template version
        /// </summary>
        [Size(50)]
        public string? Version
        {
            get => _version;
            set => SetPropertyValue(nameof(Version), ref _version, value);
        }

        /// <summary>
        /// Whether the template is active and available for use
        /// </summary>
        [Indexed]
        public bool IsActive
        {
            get => _isActive;
            set => SetPropertyValue(nameof(IsActive), ref _isActive, value);
        }

        /// <summary>
        /// When the template was created
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetPropertyValue(nameof(CreatedAt), ref _createdAt, value);
        }

        /// <summary>
        /// When the template was last updated
        /// </summary>
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetPropertyValue(nameof(UpdatedAt), ref _updatedAt, value);
        }

        /// <summary>
        /// One-to-many relationship: Template has many Projects
        /// </summary>
        [Association("Template-Projects")]
        [JsonIgnore]
        public XPCollection<Project> Projects => GetCollection<Project>(nameof(Projects));

        /// <summary>
        /// One-to-many relationship: Template has many TemplateFiles
        /// </summary>
        [Association("Template-TemplateFiles")]
        [JsonIgnore]
        public XPCollection<TemplateFile> TemplateFiles => GetCollection<TemplateFile>(nameof(TemplateFiles));

        /// <summary>
        /// One-to-many relationship: Template has many Nodes
        /// </summary>
        [Association("Template-Nodes")]
        [JsonIgnore]
        public XPCollection<Node> Nodes => GetCollection<Node>(nameof(Nodes));

        /// <summary>
        /// One-to-many relationship: Template has many Prompts
        /// </summary>
        [Association("Template-Prompts")]
        [JsonIgnore]
        public XPCollection<Prompt> Prompts => GetCollection<Prompt>(nameof(Prompts));

        /// <summary>
        /// One-to-many relationship: Template has many AIModels
        /// </summary>
        [Association("Template-AIModels")]
        [JsonIgnore]
        public XPCollection<AIModel> AIModels => GetCollection<AIModel>(nameof(AIModels));
    }
}