namespace KingOfTheColonyApi.Extensions;

public static class ConfigurationExtensions
{
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name, string environmentVariableName)
    {
        var value = configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(value) || value.Contains("[YOUR-PASSWORD]", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"ConnectionStrings:{name} must be configured. Set the {environmentVariableName} environment variable.");

        return value;
    }

    public static string GetRequiredValue(this IConfiguration configuration, string key, string environmentVariableName)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "ENV_VAR", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{key} must be configured. Set the {environmentVariableName} environment variable.");

        return value;
    }
}