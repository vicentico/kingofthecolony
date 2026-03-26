using Npgsql;

namespace KingOfTheColonyApi.Extensions;

public static class ConfigurationExtensions
{
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name, string environmentVariableName)
    {
        var value = configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)
            || value.Contains("[YOUR-PASSWORD]", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"ConnectionStrings:{name} must be configured. Set the {environmentVariableName} environment variable.");

        return NormalizePostgresConnectionString(value, environmentVariableName);
    }

    public static string? GetOptionalConnectionString(this IConfiguration configuration, string name, string environmentVariableName)
    {
        var value = configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)
            || value.Contains("[YOUR-PASSWORD]", StringComparison.OrdinalIgnoreCase))
            return null;

        return NormalizePostgresConnectionString(value, environmentVariableName);
    }

    public static string GetRequiredValue(this IConfiguration configuration, string key, string environmentVariableName)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "ENV_VAR", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{key} must be configured. Set the {environmentVariableName} environment variable.");

        return value;
    }

    private static string NormalizePostgresConnectionString(string value, string environmentVariableName)
    {
        if (value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            return ConvertPostgresUriToConnectionString(value, environmentVariableName);

        if (!value.StartsWith("jdbc:postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        var jdbcValue = value[5..];

        return ConvertPostgresUriToConnectionString(jdbcValue, environmentVariableName);
    }

    private static string ConvertPostgresUriToConnectionString(string value, string environmentVariableName)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"The {environmentVariableName} value is not a valid PostgreSQL URL.");

        var query = ParseQueryString(uri.Query);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.Trim('/'),
        };

        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            var userInfoParts = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(userInfoParts[0]);

            if (userInfoParts.Length > 1)
                builder.Password = Uri.UnescapeDataString(userInfoParts[1]);
        }

        if (query.TryGetValue("user", out var user) || query.TryGetValue("username", out user))
            builder.Username = user;

        if (query.TryGetValue("password", out var password))
            builder.Password = password;

        if (query.TryGetValue("sslmode", out var sslMode) || query.TryGetValue("ssl mode", out sslMode))
            builder.SslMode = Enum.Parse<SslMode>(sslMode, ignoreCase: true);

        return builder.ConnectionString;
    }

    private static Dictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(queryString))
            return result;

        foreach (var pair in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }
}