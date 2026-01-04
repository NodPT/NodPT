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
                // Get connection string from DatabaseHelper (converts XPO format to MySQL format)
                var connectionString = GetMySqlConnectionString();
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("Unable to get connection string");
                    return;
                }
                
                // Get the path to SQL scripts directory
                // Try multiple locations to handle different deployment scenarios
                var sqlScriptsPath = FindSqlScriptsDirectory();

                if (string.IsNullOrEmpty(sqlScriptsPath) || !Directory.Exists(sqlScriptsPath))
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
        /// Finds the SQL scripts directory by checking multiple possible locations
        /// </summary>
        private static string FindSqlScriptsDirectory()
        {
            // Option 1: Relative to application base directory (for development and Docker)
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Data", "sql-scripts");
            basePath = Path.GetFullPath(basePath);
            
            if (Directory.Exists(basePath))
            {
                return basePath;
            }

            // Option 2: Look for sql-scripts in parent directories
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                var testPath = Path.Combine(currentDir, "Data", "sql-scripts");
                if (Directory.Exists(testPath))
                {
                    return testPath;
                }
                
                var parentDir = Directory.GetParent(currentDir);
                if (parentDir == null) break;
                currentDir = parentDir.FullName;
            }

            // Option 3: Check environment variable if set
            var envPath = Environment.GetEnvironmentVariable("SQL_SCRIPTS_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            {
                return envPath;
            }

            return string.Empty;
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
        /// Gets MySQL connection string by converting XPO connection string or reading from environment
        /// </summary>
        private static string GetMySqlConnectionString()
        {
            // Try to get connection string from DatabaseHelper
            var xpoConnectionString = DatabaseHelper.GetConnectionString();
            
            if (!string.IsNullOrEmpty(xpoConnectionString))
            {
                // Convert XPO connection string to MySQL connection string
                // XPO format: XpoProvider=MySql;server=host;port=port;user=user;password=password;database=db;...
                // MySQL format: Server=host;Port=port;Database=db;User=user;Password=password;...
                
                var parts = xpoConnectionString.Split(';');
                var server = "";
                var port = "";
                var database = "";
                var user = "";
                var password = "";
                var sslMode = "Preferred";
                var charset = "utf8mb4";
                
                foreach (var part in parts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].Trim().ToLower();
                        var value = keyValue[1].Trim();
                        
                        switch (key)
                        {
                            case "server":
                                server = value;
                                break;
                            case "port":
                                port = value;
                                break;
                            case "database":
                                database = value;
                                break;
                            case "user":
                                user = value;
                                break;
                            case "password":
                                password = value;
                                break;
                            case "sslmode":
                                sslMode = value;
                                break;
                            case "charset":
                                charset = value;
                                break;
                        }
                    }
                }
                
                return $"Server={server};Port={port};Database={database};User={user};Password={password};SslMode={sslMode};CharSet={charset};";
            }
            
            // Fallback: get from environment variables
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
            var portEnv = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
            var db = Environment.GetEnvironmentVariable("DB_NAME") ?? "nodpt";
            var userEnv = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
            var passwordEnv = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

            return $"Server={host};Port={portEnv};Database={db};User={userEnv};Password={passwordEnv};SslMode=Preferred;CharSet=utf8mb4;";
        }
    }
}
