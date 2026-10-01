using Npgsql;

public static class DatabaseConnection
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("ConnectionStrings:Default is required.");

        value = value.Trim();
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("The PostgreSQL connection URL is invalid.");

        var credentials = uri.UserInfo.Split(':', 2);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (credentials.Length != 2 || string.IsNullOrEmpty(credentials[0]) ||
            string.IsNullOrEmpty(database))
            throw new InvalidOperationException("The PostgreSQL URL must include a username, password, and database.");

        // The builder quotes special characters in decoded credentials safely.
        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port == -1 ? 5432 : uri.Port,
            Database = database,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1])
        };

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = parameter.Split('=', 2);
            if (parts.Length != 2 ||
                !Uri.UnescapeDataString(parts[0]).Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The PostgreSQL URL supports only the sslmode query parameter.");

            var mode = Uri.UnescapeDataString(parts[1]).Replace("-", "");
            if (!Enum.TryParse<SslMode>(mode, true, out var sslMode) || !Enum.IsDefined(sslMode))
                throw new InvalidOperationException("The PostgreSQL URL contains an invalid sslmode.");

            connection.SslMode = sslMode;
        }

        return connection.ConnectionString;
    }
}
