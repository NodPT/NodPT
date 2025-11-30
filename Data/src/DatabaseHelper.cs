using DevExpress.Data;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NodPT.Data.Models;
using System.Runtime.CompilerServices;

public static class DatabaseHelper
{
    static string connectionString = string.Empty;
    public static WebApplicationBuilder? builder { get; set; }

    /// <summary>
    /// get the unit of work from the Services. Get and use, do not dispose it, do not use `using`
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static UnitOfWork? GetSession()
    {
        if (builder == null)
            throw new InvalidOperationException("WebApplicationBuilder is not set. Please set it before getting a session.");

        var app = builder.Build();

        // Get the UnitOfWork from the service provider
        var unitOfWork = app.Services.GetRequiredService<UnitOfWork>();
        return unitOfWork;
    }

    public static void SetBuilder(WebApplicationBuilder builder)
    {
        DatabaseHelper.builder = builder;
    }

    [Obsolete("use GetSession to get the unit of work")]
    public static UnitOfWork CreateUnitOfWork()
    {

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string is not set. Please set it before creating a UnitOfWork.");

        var dataStore = XpoDefault.GetConnectionProvider(connectionString, AutoCreateOption.SchemaAlreadyExists);
        var dl = new SimpleDataLayer(dataStore);

#if DEBUG
        //        CreateSampleData(dl);
#endif

        return new UnitOfWork(dl);

    }
    public static void SetConnectionString(string _connectionString)
    {
        connectionString = _connectionString;
    }
}
