using DevExpress.Xpo;
using System.ComponentModel;

namespace NodPT.Data.Models
{
    public class UserAccessLog : XPObject
    {
        private User? _user;
        private string? _action;
        private string? _ipAddress;
        private string? _userAgent;
        private DateTime _timestamp = DateTime.UtcNow;
        private bool _success = true;
        private string? _errorMessage;

        public UserAccessLog(Session session) : base(session) { }
        public UserAccessLog() : base(Session.DefaultSession) { }

        /// <summary>
        /// Reference to the user who performed the action
        /// </summary>
        [Association("User-AccessLogs")]
        public User? User
        {
            get => _user;
            set => SetPropertyValue(nameof(User), ref _user, value);
        }

        /// <summary>
        /// The action performed (login, logout, refresh_token)
        /// </summary>
        [Size(50)]
        public string? Action
        {
            get => _action;
            set => SetPropertyValue(nameof(Action), ref _action, value);
        }

        /// <summary>
        /// IP address of the user
        /// </summary>
        [Size(45)] // IPv6 can be up to 45 characters
        public string? IpAddress
        {
            get => _ipAddress;
            set => SetPropertyValue(nameof(IpAddress), ref _ipAddress, value);
        }

        /// <summary>
        /// User agent string from the request
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? UserAgent
        {
            get => _userAgent;
            set => SetPropertyValue(nameof(UserAgent), ref _userAgent, value);
        }

        /// <summary>
        /// When the action occurred
        /// </summary>
        [Indexed]
        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetPropertyValue(nameof(Timestamp), ref _timestamp, value);
        }

        /// <summary>
        /// Whether the action was successful
        /// </summary>
        public bool Success
        {
            get => _success;
            set => SetPropertyValue(nameof(Success), ref _success, value);
        }

        /// <summary>
        /// Error message if the action failed
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetPropertyValue(nameof(ErrorMessage), ref _errorMessage, value);
        }
    }
}