using Microsoft.EntityFrameworkCore;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services
{
    public class LogService
    {
        private readonly NodPTDbContext context;

        public LogService(NodPTDbContext dbContext)
        {
            this.context = dbContext;
        }

        public List<LogDto> GetAllLogs()
        {
            return context.Logs
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new LogDto
                {
                    Id = l.Id,
                    ErrorMessage = l.ErrorMessage,
                    StackTrace = l.StackTrace,
                    Username = l.Username,
                    Timestamp = l.Timestamp,
                    Controller = l.Controller,
                    Action = l.Action
                }).ToList();
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
            using var context = DatabaseHelper.CreateDbContext();
            using var transaction = context.Database.BeginTransaction();

            try
            {
                var log = new Log
                {
                    ErrorMessage = errorMessage,
                    StackTrace = stackTrace,
                    Username = username,
                    Timestamp = DateTime.UtcNow,
                    Controller = controller,
                    Action = action
                };

                context.Logs.Add(log);
                context.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                // If logging fails, we don't want to throw another exception
                // Log to console as fallback
                Console.WriteLine($"Failed to log error: {errorMessage}. Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
