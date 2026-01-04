using DevExpress.Xpo;
using MySqlConnector;
using NodPT.Data.Models;
using System;
using System.IO;
using System.Linq;

namespace NodPT.Data
{
    public class DemoDataHelper
    {
        private readonly string _connectionString;
        private readonly UnitOfWork _session;

        /// <summary>
        /// Initializes a new instance of DemoDataHelper with the provided connection string
        /// </summary>
        /// <param name="connectionString">MySQL connection string for executing SQL scripts</param>
        /// <param name="session">UnitOfWork session for checking existing data</param>
        public DemoDataHelper(string connectionString, UnitOfWork session)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Creates sample data by executing SQL scripts from Data/sql-scripts directory.
        /// Checks if data already exists before executing scripts to avoid duplicates.
        /// </summary>
        public void CreateSampleData()
        {
            Console.WriteLine("Checking for existing sample data...");

            try
            {
                // Get the path to SQL scripts directory
                var sqlScriptsPath = FindSqlScriptsDirectory();

                if (string.IsNullOrEmpty(sqlScriptsPath) || !Directory.Exists(sqlScriptsPath))
                {
                    Console.WriteLine($"SQL scripts directory not found: {sqlScriptsPath}");
                    return;
                }

                // Check and execute templates script
                if (!_session.Query<Template>().Any())
                {
                    var templatesFile = Path.Combine(sqlScriptsPath, "01_sample_data_templates.sql");
                    if (File.Exists(templatesFile))
                    {
                        Console.WriteLine("Creating sample templates...");
                        ExecuteSqlScript(templatesFile);
                    }
                    else
                    {
                        Console.WriteLine("Templates SQL script not found");
                        return; // Cannot proceed without templates
                    }
                }
                else
                {
                    Console.WriteLine("Templates already exist - skipping template creation");
                }

                // Check and execute prompts script
                if (!_session.Query<Prompt>().Any())
                {
                    var promptsFile = Path.Combine(sqlScriptsPath, "02_sample_data_prompts.sql");
                    if (File.Exists(promptsFile))
                    {
                        Console.WriteLine("Creating sample prompts...");
                        ExecuteSqlScript(promptsFile);
                    }
                    else
                    {
                        Console.WriteLine("Prompts SQL script not found");
                    }
                }
                else
                {
                    Console.WriteLine("Prompts already exist - skipping prompt creation");
                }

                // Check and execute AI models script
                if (!_session.Query<AIModel>().Any())
                {
                    var aiModelsFile = Path.Combine(sqlScriptsPath, "03_sample_data_aimodels.sql");
                    if (File.Exists(aiModelsFile))
                    {
                        Console.WriteLine("Creating sample AI models...");
                        ExecuteSqlScript(aiModelsFile);
                    }
                    else
                    {
                        Console.WriteLine("AI Models SQL script not found");
                    }
                }
                else
                {
                    Console.WriteLine("AI Models already exist - skipping AI model creation");
                }

                Console.WriteLine("Sample data creation completed successfully");
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
        private void ExecuteSqlScript(string scriptPath)
        {
            try
            {
                var sqlContent = File.ReadAllText(scriptPath);
                
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Split the SQL content by semicolons to execute individual statements
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
    }
}
