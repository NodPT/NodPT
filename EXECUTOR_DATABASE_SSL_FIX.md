# Executor Database SSL Connection Fix

## Problem

The Executor service was experiencing intermittent MySQL/MariaDB authentication failures with the following error:

```
System.AggregateException: Authentication to host 'localhost' failed. (I/O error occurred.)
 ---> System.IO.IOException: I/O error occurred.
   at MySql.Data.Common.Ssl.<StartSSLAsync>b__10_1()
```

This error occurred at line 326 of `ChatStreamWorker.cs` when trying to add data to Redis after processing a chat message. However, the root cause was not Redis-related but rather a MySQL SSL connection timeout.

## Root Cause

The connection string in both `WebAPI/src/Services/DatabaseInitializer.cs` and `Executor/src/Services/DatabaseInitializer.cs` used `SslMode=Preferred`, which:

1. Attempts to establish an SSL/TLS connection to the MySQL server first
2. Falls back to unencrypted connection if SSL fails
3. During SSL negotiation, if the handshake times out or fails, it throws an I/O error
4. This error propagates and appears to fail at the Redis operation that follows

## Solution

Changed the MySQL connection string SSL mode from `SslMode=Preferred` to `SslMode=None` in both services:

### Before
```csharp
var connectionString = $"XpoProvider=MySql;server={host};port={port};user={user};password={password};database={db};SslMode=Preferred;Pooling=true;CharSet=utf8mb4;";
```

### After
```csharp
var connectionString = $"XpoProvider=MySql;server={host};port={port};user={user};password={password};database={db};SslMode=None;Pooling=true;CharSet=utf8mb4;";
```

## Rationale

1. **Docker Internal Network**: The WebAPI, Executor, and Database containers communicate over an internal Docker network (`backend_network`), which is already isolated from external networks
2. **Performance**: SSL adds overhead to database operations, and within a trusted Docker network, it's unnecessary
3. **Reliability**: Removes SSL handshake failures that can occur due to misconfiguration or timeout issues
4. **Consistency**: Both WebAPI and Executor now use the same SSL mode setting

## Alternative SSL Modes

If SSL is required for compliance or security policies:

- `SslMode=Required`: Forces SSL connection (fails if SSL not available)
- `SslMode=VerifyCA`: Requires valid SSL certificate signed by trusted CA
- `SslMode=VerifyFull`: Strictest mode, validates certificate and hostname

For production deployments with external database connections, consider using `SslMode=Required` or stricter modes with proper certificate configuration.

## Files Modified

1. `Executor/src/Services/DatabaseInitializer.cs` - Line 28
2. `WebAPI/src/Services/DatabaseInitializer.cs` - Line 21

## Testing

To verify the fix:

1. Start the Executor service
2. Send a chat message through the WebAPI
3. Monitor Executor logs for successful processing without authentication errors
4. Verify Redis stream receives the result without I/O errors

## Related

- Redis configuration is correct and uses the same pattern in both services
- Redis connection uses `AbortOnConnectFail=false` for resilience
- The error appeared at Redis Add but was actually a database SSL issue that occurred earlier in the transaction
