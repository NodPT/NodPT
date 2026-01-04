using DevExpress.Xpo;
using MySqlConnector;
using NodPT.Data.Models;
using System;
using System.IO;
using System.Linq;

namespace NodPT.Data
{
    public static class DemoDataHelper
    {
        /// <summary>
        /// Creates sample data by executing SQL scripts from Data/sql-scripts directory.
        /// Checks if data already exists before executing scripts to avoid duplicates.
        /// </summary>
        public static void CreateSampleData()
        {
            var session = DatabaseHelper.GetSession();
            if (session == null)
            {
                Console.WriteLine("Unable to get database session");
                return;
            }

            // Check if sample data already exists
            if (session.Query<Template>().Any())
            {
                Console.WriteLine("Sample data already exists - skipping SQL script execution");
                return;
            }

            Console.WriteLine("Creating sample data from SQL scripts...");

            try
            {
                // Get connection string from DatabaseHelper
                var connectionString = GetConnectionStringFromEnvironment();
                
                // Get the path to SQL scripts directory
                var sqlScriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Data", "sql-scripts");
                sqlScriptsPath = Path.GetFullPath(sqlScriptsPath);

                if (!Directory.Exists(sqlScriptsPath))
                {
                    Console.WriteLine($"SQL scripts directory not found: {sqlScriptsPath}");
                    return;
                }

                // Execute SQL scripts in order
                var sqlFiles = new[]
                {
                    "01_sample_data_templates.sql",
                    "02_sample_data_prompts.sql",
                    "03_sample_data_aimodels.sql"
                };

                foreach (var sqlFile in sqlFiles)
                {
                    var filePath = Path.Combine(sqlScriptsPath, sqlFile);
                    if (File.Exists(filePath))
                    {
                        Console.WriteLine($"Executing SQL script: {sqlFile}");
                        ExecuteSqlScript(connectionString, filePath);
                    }
                    else
                    {
                        Console.WriteLine($"SQL script not found: {filePath}");
                    }
                }

                Console.WriteLine("Sample data created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating sample data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Executes a SQL script file against the database
        /// </summary>
        private static void ExecuteSqlScript(string connectionString, string scriptPath)
        {
            try
            {
                var sqlContent = File.ReadAllText(scriptPath);
                
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // Split the SQL content by semicolons to execute individual statements
                    // This is a simple approach - more complex SQL might need a better parser
                    var statements = sqlContent.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var statement in statements)
                    {
                        var trimmedStatement = statement.Trim();
                        
                        // Skip empty statements and comments
                        if (string.IsNullOrWhiteSpace(trimmedStatement) || 
                            trimmedStatement.StartsWith("--") ||
                            trimmedStatement.StartsWith("/*"))
                        {
                            continue;
                        }

                        // Skip SOURCE commands (they're for MySQL client only)
                        if (trimmedStatement.ToUpper().StartsWith("SOURCE"))
                        {
                            continue;
                        }

                        try
                        {
                            using (var command = new MySqlCommand(trimmedStatement, connection))
                            {
                                command.CommandTimeout = 60; // 60 seconds timeout
                                command.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error executing statement: {trimmedStatement.Substring(0, Math.Min(100, trimmedStatement.Length))}...");
                            Console.WriteLine($"Error: {ex.Message}");
                            // Continue with next statement instead of failing completely
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing SQL script {scriptPath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets MySQL connection string from environment variables
        /// </summary>
        private static string GetConnectionStringFromEnvironment()
        {
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
            var db = Environment.GetEnvironmentVariable("DB_NAME") ?? "nodpt";
            var user = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

            return $"Server={host};Port={port};Database={db};User={user};Password={password};SslMode=Preferred;CharSet=utf8mb4;";
        }
    }
}
