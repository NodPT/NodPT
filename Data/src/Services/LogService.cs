using DevExpress.Xpo;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class LogService
    {
        private readonly UnitOfWork session;

        public LogService(UnitOfWork unitOfWork)
        {
            this.session = unitOfWork;
        }

        public List<LogDto> GetAllLogs()
        {
            var logs = new XPCollection<Log>(session);

            return logs.Select(l => new LogDto
            {
                Id = l.Oid,
                ErrorMessage = l.ErrorMessage,
                StackTrace = l.StackTrace,
                Username = l.Username,
                Timestamp = l.Timestamp,
                Controller = l.Controller,
                Action = l.Action
            }).OrderByDescending(l => l.Timestamp).ToList();
        }


        public static void LogError(Exception err, string? username = "", string? controller = "", string? action = "")
        {
            LogError(err.Message, err.StackTrace, username, controller, action);
        }

        public static void LogError(Exception err)
        {
            LogError(err.Message, err.StackTrace, null, null, null);
        }

        public static void LogError(string errorMessage, string? stackTrace, string? username, string? controller, string? action)
        {
            using var session = DatabaseHelper.CreateUnitOfWork();
            session.BeginTransaction();

            try
            {
                var log = new Log(session)
                {
                    ErrorMessage = errorMessage,
                    StackTrace = stackTrace,
                    Username = username,
                    Timestamp = DateTime.UtcNow,
                    Controller = controller,
                    Action = action
                };

                session.Save(log);
                session.CommitTransaction();
            }
            catch (Exception ex)
            {
                session.RollbackTransaction();
                // If logging fails, we don't want to throw another exception
                // Log to console as fallback
                Console.WriteLine($"Failed to log error: {errorMessage}. Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
