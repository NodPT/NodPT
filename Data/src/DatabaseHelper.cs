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
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// get the unit of work from the Services. Get and use, do not dispose it, do not use `using`
    /// NOTE: This creates a new UnitOfWork from the fallback connection string.
    /// Prefer using dependency injection or HttpContext.RequestServices.GetRequiredService<UnitOfWork>() instead.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static UnitOfWork? GetSession()
    {
        // Fallback to creating a UnitOfWork directly when service provider is not available
        // This is needed for background services and console applications
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string is not set. Please set it before creating a UnitOfWork.");

        var dataStore = XpoDefault.GetConnectionProvider(connectionString, AutoCreateOption.SchemaAlreadyExists);
        var dl = new SimpleDataLayer(dataStore);
        return new UnitOfWork(dl);
    }

    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
