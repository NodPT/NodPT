using DevExpress.Xpo;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class TemplateFile : XPObject
    {
        private string? _name;
        private string? _path;
        private string? _extension;
        private long _size = 0;
        private string? _mimeType;
        private string? _content;
        private Template? _template;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;

        public TemplateFile(Session session) : base(session) { }
        public TemplateFile() : base(Session.DefaultSession) { }

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
        /// Full path of the file relative to template root
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
        /// Many-to-one relationship: TemplateFile belongs to one Template
        /// </summary>
        [Association("Template-TemplateFiles")]
        [JsonIgnore]
        public Template? Template
        {
            get => _template;
            set => SetPropertyValue(nameof(Template), ref _template, value);
        }
    }
}