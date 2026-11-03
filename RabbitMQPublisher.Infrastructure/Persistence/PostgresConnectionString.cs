namespace RabbitMQPublisher.Infrastructure.Persistence;

/// <summary>
/// Normalizes the PostgreSQL connection string, accepting both formats used in deployments:
/// the Npgsql key/value format (<c>Host=...;Port=...;Database=...;Username=...;Password=...</c>) and the
/// URL format shown by Coolify and other platforms (<c>postgres://user:password@host:port/database</c>).
/// </summary>
internal static class PostgresConnectionString
{
    private const int DefaultPort = 5432;

    /// <summary>
    /// Returns the connection string in the Npgsql key/value format, converting it when it is a URL.
    /// </summary>
    /// <param name="name">Name of the connection string, used in the error message.</param>
    /// <param name="value">Configured connection string.</param>
    /// <returns>The connection string in the Npgsql key/value format.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value is not a valid connection string. The message never includes the value, because it contains the password.
    /// </exception>
    public static string Normalize(string name, string value)
    {
        string trimmed = value.Trim();

        try
        {
            NpgsqlConnectionStringBuilder builder = IsUrl(trimmed) ? FromUrl(trimmed) : new NpgsqlConnectionStringBuilder(trimmed);
            if (string.IsNullOrWhiteSpace(builder.Host))
                throw new ArgumentException("The connection string does not define a host.");

            return builder.ConnectionString;
        }
        catch (Exception exception) when (exception is ArgumentException or UriFormatException or KeyNotFoundException)
        {
            throw new InvalidOperationException(
                $"The connection string '{name}' is not a valid PostgreSQL connection string. Use 'Host=...;Port=...;Database=...;Username=...;Password=...' or 'postgres://user:password@host:port/database'.");
        }
    }

    private static bool IsUrl(string value)
        => value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Converts <c>postgres://user:password@host:port/database?key=value</c> into a connection string builder.
    /// User, password and database are URL-decoded; query parameters are passed as Npgsql keywords (for example <c>sslmode=require</c>).
    /// </summary>
    private static NpgsqlConnectionStringBuilder FromUrl(string value)
    {
        Uri uri = new(value);
        string[] userInfo = uri.UserInfo.Split(':', 2);

        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? DefaultPort : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null
        };

        foreach (string parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = parameter.Split('=', 2);
            builder[Uri.UnescapeDataString(pair[0])] = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
        }

        return builder;
    }
}
