using DevExpress.Xpo;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class Folder : XPObject
    {
        private string? _name;
        private string? _path;
        private Folder? _parent;
        private Project? _project;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;

        public Folder(Session session) : base(session) { }
        public Folder() : base(Session.DefaultSession) { }

        /// <summary>
        /// Folder name
        /// </summary>
        [Size(255)]
        [Indexed]
        public string? Name
        {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        /// <summary>
        /// Full path of the folder relative to project root
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        [Indexed]
        public string? Path
        {
            get => _path;
            set => SetPropertyValue(nameof(Path), ref _path, value);
        }

        /// <summary>
        /// When the folder was created
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetPropertyValue(nameof(CreatedAt), ref _createdAt, value);
        }

        /// <summary>
        /// When the folder was last updated
        /// </summary>
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetPropertyValue(nameof(UpdatedAt), ref _updatedAt, value);
        }

        /// <summary>
        /// Many-to-one relationship: Folder belongs to one Project
        /// </summary>
        [Association("Project-Folders")]
        [JsonIgnore]
        public Project? Project
        {
            get => _project;
            set => SetPropertyValue(nameof(Project), ref _project, value);
        }

        /// <summary>
        /// Self-referencing relationship: Folder can have a parent folder
        /// </summary>
        [Association("ParentFolder-ChildFolders")]
        [JsonIgnore]
        public Folder? Parent
        {
            get => _parent;
            set => SetPropertyValue(nameof(Parent), ref _parent, value);
        }

        /// <summary>
        /// Self-referencing relationship: Folder can have child folders
        /// </summary>
        [Association("ParentFolder-ChildFolders")]
        [JsonIgnore]
        public XPCollection<Folder> Children => GetCollection<Folder>(nameof(Children));

        /// <summary>
        /// One-to-many relationship: Folder has many Files
        /// </summary>
        [Association("Folder-Files")]
        [JsonIgnore]
        public XPCollection<ProjectFile> Files => GetCollection<ProjectFile>(nameof(Files));
    }
}