using DevExpress.Xpo;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class ProjectFile : XPObject
    {
        private string? _name;
        private string? _path;
        private string? _extension;
        private long _size = 0;
        private string? _mimeType;
        private string? _content;
        private Folder? _folder;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;

        public ProjectFile(Session session) : base(session) { }
        public ProjectFile() : base(Session.DefaultSession) { }

        /// <summary>
        /// File name including extension
        /// </summary>
        [Size(255)]
        [Indexed]
        public string? Name
        {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        /// <summary>
        /// Full path of the file relative to project root
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        [Indexed]
        public string? Path
        {
            get => _path;
            set => SetPropertyValue(nameof(Path), ref _path, value);
        }

        /// <summary>
        /// File extension (e.g., .txt, .js, .py)
        /// </summary>
        [Size(50)]
        [Indexed]
        public string? Extension
        {
            get => _extension;
            set => SetPropertyValue(nameof(Extension), ref _extension, value);
        }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size
        {
            get => _size;
            set => SetPropertyValue(nameof(Size), ref _size, value);
        }

        /// <summary>
        /// MIME type of the file
        /// </summary>
        [Size(100)]
        public string? MimeType
        {
            get => _mimeType;
            set => SetPropertyValue(nameof(MimeType), ref _mimeType, value);
        }

        /// <summary>
        /// File content (for text files)
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? Content
        {
            get => _content;
            set => SetPropertyValue(nameof(Content), ref _content, value);
        }

        /// <summary>
        /// When the file was created
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetPropertyValue(nameof(CreatedAt), ref _createdAt, value);
        }

        /// <summary>
        /// When the file was last updated
        /// </summary>
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetPropertyValue(nameof(UpdatedAt), ref _updatedAt, value);
        }

        /// <summary>
        /// Many-to-one relationship: File belongs to one Folder
        /// </summary>
        [Association("Folder-Files")]
        [JsonIgnore]
        public Folder? Folder
        {
            get => _folder;
            set => SetPropertyValue(nameof(Folder), ref _folder, value);
        }
    }
}