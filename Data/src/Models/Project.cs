using DevExpress.Xpo;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class Project : XPObject
    {
        private string? _name;
        private string? _description;
        private bool _isActive = true;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;
        private User? _user;
        private Template? _template;

        public Project(Session session) : base(session) { }
        public Project() : base(Session.DefaultSession) { }

        /// <summary>
        /// Project name
        /// </summary>
        [Size(255)]
        [Indexed]
        public string? Name
        {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        /// <summary>
        /// Project description
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? Description
        {
            get => _description;
            set => SetPropertyValue(nameof(Description), ref _description, value);
        }

        /// <summary>
        /// Whether the project is active
        /// </summary>
        [Indexed]
        public bool IsActive
        {
            get => _isActive;
            set => SetPropertyValue(nameof(IsActive), ref _isActive, value);
        }

        /// <summary>
        /// When the project was created
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetPropertyValue(nameof(CreatedAt), ref _createdAt, value);
        }

        /// <summary>
        /// When the project was last updated
        /// </summary>
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetPropertyValue(nameof(UpdatedAt), ref _updatedAt, value);
        }

        /// <summary>
        /// Many-to-one relationship: Project belongs to one User
        /// </summary>
        [Association("User-Projects")]
        [JsonIgnore]
        public User? User
        {
            get => _user;
            set => SetPropertyValue(nameof(User), ref _user, value);
        }

        /// <summary>
        /// Many-to-one relationship: Project uses one Template
        /// </summary>
        [Association("Template-Projects")]
        [JsonIgnore]
        public Template? Template
        {
            get => _template;
            set => SetPropertyValue(nameof(Template), ref _template, value);
        }

        /// <summary>
        /// One-to-many relationship: Project has many Nodes
        /// </summary>
        [Association("Project-Nodes")]
        [JsonIgnore]
        public XPCollection<Node> Nodes => GetCollection<Node>(nameof(Nodes));

        /// <summary>
        /// One-to-many relationship: Project has many Folders
        /// </summary>
        [Association("Project-Folders")]
        [JsonIgnore]
        public XPCollection<Folder> Folders => GetCollection<Folder>(nameof(Folders));
    }
}