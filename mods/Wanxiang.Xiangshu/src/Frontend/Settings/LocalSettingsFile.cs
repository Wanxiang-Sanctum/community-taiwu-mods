using Newtonsoft.Json;

namespace Wanxiang.Xiangshu.Frontend.Settings;

internal static class LocalSettingsFile
{
    private const string FileName = "LocalSettings.json";

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static string GetPath(string modDirectory)
    {
        return Path.Combine(modDirectory, FileName);
    }

    public static IReadOnlyList<AgentEnvironmentVariable> LoadAgentEnvironmentVariables(string modDirectory)
    {
        string path = GetPath(modDirectory);
        if (!File.Exists(path))
        {
            return [];
        }

        LocalSettingsDocument settings = LoadSettings(path);
        IReadOnlyDictionary<string, string?>? environmentVariables = settings.Agent?.Env;
        if (environmentVariables is null)
        {
            return [];
        }

        List<AgentEnvironmentVariable> variables = [];
        foreach (KeyValuePair<string, string?> variable in environmentVariables)
        {
            string name = variable.Key.Trim();
            if (!IsValidName(name))
            {
                throw CreateFormatException(path, "Environment variable names must be non-empty and must not contain whitespace or '='.");
            }

            if (variable.Value is null)
            {
                throw CreateFormatException(path, $"Environment variable '{name}' must use a string value.");
            }

            variables.Add(new AgentEnvironmentVariable(
                name,
                variable.Value));
        }

        return variables;
    }

    private static LocalSettingsDocument LoadSettings(string path)
    {
        try
        {
            LocalSettingsDocument settings = new();
            JsonConvert.PopulateObject(
                File.ReadAllText(path),
                settings,
                JsonSettings);
            return settings;
        }
        catch (JsonException ex)
        {
            throw CreateFormatException(path, ex.Message, ex);
        }
    }

    private static bool IsValidName(string name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        foreach (char character in name)
        {
            if (character == '=' || char.IsWhiteSpace(character))
            {
                return false;
            }
        }

        return true;
    }

    private static FormatException CreateFormatException(
        string path,
        string message,
        Exception? innerException = null)
    {
        return new FormatException(
            $"Invalid Wanxiang.Xiangshu local settings file '{path}': {message}",
            innerException);
    }

    private sealed class LocalSettingsDocument
    {
        [JsonProperty("agent")]
        public LocalAgentSettings? Agent { get; set; } = new();
    }

    private sealed class LocalAgentSettings
    {
        [JsonProperty("env")]
        public Dictionary<string, string?>? Env { get; set; } = [];
    }
}

internal sealed class AgentEnvironmentVariable(
    string name,
    string value)
{
    public string Name { get; } = name;

    public string Value { get; } = value;
}
