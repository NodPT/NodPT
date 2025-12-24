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
    private static volatile IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Set the IHttpContextAccessor for resolving request-scoped UnitOfWork instances.
    /// This should be called once during application startup.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when accessor is null</exception>
    public static void SetHttpContextAccessor(IHttpContextAccessor accessor)
    {
        if (accessor == null)
            throw new ArgumentNullException(nameof(accessor));
        
        _httpContextAccessor = accessor;
    }

    /// <summary>
    /// Gets a <see cref="UnitOfWork"/> instance for data access.
    /// <para>
    /// <b>Web requests (when HttpContext is available):</b><br/>
    /// Returns a request-scoped <c>UnitOfWork</c> from the DI container. <b>Do not dispose</b> or use <c>using</c>—the DI container manages its lifetime.
    /// </para>
    /// <para>
    /// <b>Background services or when HttpContext is not available:</b><br/>
    /// Creates a new <c>UnitOfWork</c> instance. <b>The caller is responsible for disposing</b> the returned object (e.g., via <c>using</c> or calling <c>Dispose()</c>).
    /// </para>
    /// </summary>
    /// <returns>A <see cref="UnitOfWork"/> instance. Caller must dispose if not in web request context.</returns>
    /// <exception cref="InvalidOperationException">Thrown if connection string is not set.</exception>
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

    /// <summary>
    /// Updates the database schema to match the registered entity types.
    /// Creates missing tables and columns as needed.
    /// </summary>
    /// <param name="types">Array of entity types to create tables for</param>
    /// <exception cref="InvalidOperationException">Thrown if connection string is not set</exception>
    public static void UpdateSchema(Type[] types)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string is not set. Please set it before updating schema.");

        var dataStore = XpoDefault.GetConnectionProvider(connectionString, AutoCreateOption.DatabaseAndSchema);
        using (var dataLayer = new SimpleDataLayer(dataStore))
        {
            using (var uow = new UnitOfWork(dataLayer))
            {
                uow.UpdateSchema(types);
                uow.CreateObjectTypeRecords(types);
            }
        }
    }
}
