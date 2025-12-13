using DevExpress.Data;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using NodPT.Data.Models;
using System.Runtime.CompilerServices;

public static class DatabaseHelper
{
    static string connectionString = string.Empty;
    private static IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Set the IHttpContextAccessor for resolving request-scoped UnitOfWork instances.
    /// This should be called once during application startup.
    /// </summary>
    public static void SetHttpContextAccessor(IHttpContextAccessor accessor)
    {
        _httpContextAccessor = accessor;
    }

    /// <summary>
    /// Get the unit of work from the Services. Get and use, do not dispose it, do not use `using`
    /// NOTE: For web requests, this uses HttpContext.RequestServices to get request-scoped UnitOfWork.
    /// For background services without HttpContext, it creates a new UnitOfWork from connection string.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static UnitOfWork? GetSession()
    {
        // Try to get UnitOfWork from request scope if HttpContext is available
        if (_httpContextAccessor?.HttpContext != null)
        {
            return _httpContextAccessor.HttpContext.RequestServices.GetRequiredService<UnitOfWork>();
        }

        // Fallback to creating a UnitOfWork directly when HttpContext is not available
        // This is needed for background services and console applications
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string is not set. Please set it before creating a UnitOfWork.");

        var dataStore = XpoDefault.GetConnectionProvider(connectionString, AutoCreateOption.SchemaAlreadyExists);
        var dl = new SimpleDataLayer(dataStore);
        return new UnitOfWork(dl);
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
