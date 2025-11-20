using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using NodPT.Data.Services;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class User : XPObject
    {
        private string? _firebaseUid;
        private string? _email;
        private string? _displayName;
        private string? _photoUrl;
        private bool _active = true;
        private bool _approved = false;
        private bool _banned = false;
        private bool _isAdmin = false;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _lastLoginAt = DateTime.UtcNow;
        private string? _refreshToken;

        public User(Session session) : base(session) { }
        public User() : base(Session.DefaultSession) { }

        /// <summary>
        /// Firebase User ID - unique identifier from Firebase Authentication
        /// </summary>
        [Size(128)]
        [Indexed(Unique = true)]
        public string? FirebaseUid
        {
            get => _firebaseUid;
            set => SetPropertyValue(nameof(FirebaseUid), ref _firebaseUid, value);
        }

        /// <summary>
        /// User's email address from Firebase Authentication
        /// </summary>
        [Size(255)]
        [Indexed]
        public string? Email
        {
            get => _email;
            set => SetPropertyValue(nameof(Email), ref _email, value);
        }

        /// <summary>
        /// User's display name from Firebase Authentication
        /// </summary>
        [Size(255)]
        public string? DisplayName
        {
            get => _displayName;
            set => SetPropertyValue(nameof(DisplayName), ref _displayName, value);
        }

        /// <summary>
        /// User's profile photo URL from Firebase Authentication
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? PhotoUrl
        {
            get => _photoUrl;
            set => SetPropertyValue(nameof(PhotoUrl), ref _photoUrl, value);
        }

        /// <summary>
        /// Whether the user account is active
        /// </summary>
        [Indexed]
        public bool Active
        {
            get => _active;
            set => SetPropertyValue(nameof(Active), ref _active, value);
        }

        /// <summary>
        /// Whether the user account is approved by site admin
        /// </summary>
        [Indexed]
        public bool Approved
        {
            get => _approved;
            set => SetPropertyValue(nameof(Approved), ref _approved, value);
        }

        /// <summary>
        /// Whether the user account is banned
        /// </summary>
        [Indexed]
        public bool Banned
        {
            get => _banned;
            set => SetPropertyValue(nameof(Banned), ref _banned, value);
        }

        /// <summary>
        /// Whether the user has administrator privileges
        /// </summary>
        [Indexed]
        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetPropertyValue(nameof(IsAdmin), ref _isAdmin, value);
        }

        /// <summary>
        /// When the user account was created
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetPropertyValue(nameof(CreatedAt), ref _createdAt, value);
        }

        /// <summary>
        /// When the user last logged in
        /// </summary>
        public DateTime LastLoginAt
        {
            get => _lastLoginAt;
            set => SetPropertyValue(nameof(LastLoginAt), ref _lastLoginAt, value);
        }

        /// <summary>
        /// Firebase refresh token for maintaining authentication
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? RefreshToken
        {
            get => _refreshToken;
            set => SetPropertyValue(nameof(RefreshToken), ref _refreshToken, value);
        }

        /// <summary>
        /// One-to-many relationship: User has many ChatMessages
        /// </summary>
        [Association("User-ChatMessages")]
        [JsonIgnore]
        public XPCollection<ChatMessage> ChatMessages => GetCollection<ChatMessage>(nameof(ChatMessages));

        /// <summary>
        /// One-to-many relationship: User has many Projects
        /// </summary>
        [Association("User-Projects")]
        [JsonIgnore]
        public XPCollection<Project> Projects => GetCollection<Project>(nameof(Projects));

        /// <summary>
        /// One-to-many relationship: User has many AccessLogs
        /// </summary>
        [Association("User-AccessLogs")]
        [JsonIgnore]
        public XPCollection<UserAccessLog> AccessLogs => GetCollection<UserAccessLog>(nameof(AccessLogs));

    }
}