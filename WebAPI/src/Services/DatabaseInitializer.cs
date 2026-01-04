using DevExpress.Xpo;
using DevExpress.Data;
using DevExpress.Xpo.DB;
using Microsoft.Extensions.DependencyInjection;
using NodPT.Data.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using DevExpress.Xpo.Helpers;


public static class DatabaseInitializer
{
    static string connectionString = string.Empty;
    public static void Initialize(WebApplicationBuilder builder)
    {
        // Do NOT use defaults. Require all parts to be provided via env vars or configuration.
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT");
        var db = Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        connectionString = $"XpoProvider=MySql;server={host};port={port};user={user};password={password};database={db};SslMode=Preferred;Pooling=true;CharSet=utf8mb4;";
        DatabaseHelper.SetConnectionString(connectionString);

        builder.Services.AddXpoDefaultUnitOfWork(true, options =>
            options.UseConnectionString(connectionString)
#if DEBUG
                .UseAutoCreationOption(AutoCreateOption.DatabaseAndSchema)
#else
                .UseAutoCreationOption(AutoCreateOption.SchemaAlreadyExists)
#endif
                // Register known entity types used by the application so XPO can discover mappings.
                // StatisticInfo was not defined in the project; explicitly register the real model types.
                .UseEntityTypes(new Type[] {
                        typeof(User),
                        typeof(Node),
                        typeof(Template),
                        typeof(TemplateFile),
                        typeof(Project),
                        typeof(ProjectFile),
                        typeof(Folder),
                        typeof(ChatMessage),
                        typeof(Log),
                        typeof(UserAccessLog),
                        typeof(NodeMemory),
                        typeof(AIModel),
                        typeof(ChatResponse),
                        typeof(Prompt)
                }));

        // Make IHttpContextAccessor available for ConfigureJsonOptions which requires it.
        builder.Services.AddHttpContextAccessor();
        builder.Services.ConfigureOptions<ConfigureJsonOptions>();
        builder.Services.AddSingleton(typeof(IModelMetadataProvider), typeof(XpoMetadataProvider));

        // Create sample data
#if DEBUG
        CreateSampleData();
#endif
    }

    private static void CreateSampleData()
    {
        try
        {
            // Convert XPO connection string to MySQL format
            var mysqlConnectionString = ConvertToMySqlConnectionString(connectionString);
            
            // Get a session to check for existing data
            var session = DatabaseHelper.GetSession();
            if (session == null)
            {
                Console.WriteLine("Unable to get database session for sample data creation");
                return;
            }

            // Create DemoDataHelper instance and execute
            var demoDataHelper = new NodPT.Data.DemoDataHelper(mysqlConnectionString, session);
            demoDataHelper.CreateSampleData();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing sample data: {ex.Message}");
        }
    }

    private static string ConvertToMySqlConnectionString(string xpoConnectionString)
    {
        // Parse XPO connection string to extract values
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
            var keyValue = part.Split(new[] { '=' }, 2); // Split with limit of 2 to handle values containing '='
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
}