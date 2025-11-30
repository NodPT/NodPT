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
                .UseAutoCreationOption(AutoCreateOption.DatabaseAndSchema)
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
                        typeof(UserAccessLog)
                }));

        // Make IHttpContextAccessor available for ConfigureJsonOptions which requires it.
        builder.Services.AddHttpContextAccessor();
        builder.Services.ConfigureOptions<ConfigureJsonOptions>();
        builder.Services.AddSingleton(typeof(IModelMetadataProvider), typeof(XpoMetadataProvider));

        // Create sample data
        //CreateSampleData();
    }


}