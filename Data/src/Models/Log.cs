using DevExpress.Xpo;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class Log : XPObject
    {
        private string? _errorMessage;
        private string? _stackTrace;
        private string? _username;
        private DateTime _timestamp = DateTime.UtcNow;
        private string? _controller;
        private string? _action;

        public Log(Session session) : base(session) { }
        public Log() : base(Session.DefaultSession) { }

        /// <summary>
        /// Error message
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetPropertyValue(nameof(ErrorMessage), ref _errorMessage, value);
        }

        /// <summary>
        /// Stack trace of the error
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string? StackTrace
        {
            get => _stackTrace;
            set => SetPropertyValue(nameof(StackTrace), ref _stackTrace, value);
        }

        /// <summary>
        /// Username of the user who encountered the error
        /// </summary>
        [Size(255)]
        [Indexed]
        public string? Username
        {
            get => _username;
            set => SetPropertyValue(nameof(Username), ref _username, value);
        }

        /// <summary>
        /// When the error occurred
        /// </summary>
        [Indexed]
        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetPropertyValue(nameof(Timestamp), ref _timestamp, value);
        }

        /// <summary>
        /// Controller where the error occurred
        /// </summary>
        [Size(255)]
        public string? Controller
        {
            get => _controller;
            set => SetPropertyValue(nameof(Controller), ref _controller, value);
        }

        /// <summary>
        /// Action/Method where the error occurred
        /// </summary>
        [Size(255)]
        public string? Action
        {
            get => _action;
            set => SetPropertyValue(nameof(Action), ref _action, value);
        }
    }
}
