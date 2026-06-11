using System.Globalization;
using System.Reflection;
using GameData.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Wanxiang.Taiwu.Logging;

public sealed class TaiwuLogger
{
    private readonly string _tag;

    private TaiwuLogger(string tag)
    {
        _tag = ValidateTag(tag);
    }

    public static TaiwuLogger ForTag(string tag)
    {
        return new TaiwuLogger(tag);
    }

    public void Info(string message, object? context = null)
    {
        Write(LogLevel.Info, exception: null, message, context);
    }

    public void Warning(string message, object? context = null)
    {
        Write(LogLevel.Warning, exception: null, message, context);
    }

    public void Error(string message, object? context = null)
    {
        Write(LogLevel.Error, exception: null, message, context);
    }

    public void Error(Exception exception, string message, object? context = null)
    {
        Write(LogLevel.Error, exception, message, context);
    }

    private static string ValidateTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Log tag must not be empty.", nameof(tag));
        }

        return tag.Trim();
    }

    private void Write(
        LogLevel level,
        Exception? exception,
        string message,
        object? context)
    {
        string formattedMessage = LogLineFormatter.Format(message, exception, context);

        switch (level)
        {
            case LogLevel.Info:
                AdaptableLog.TagInfo(_tag, formattedMessage);
                break;
            case LogLevel.Warning:
                AdaptableLog.TagWarning(_tag, formattedMessage);
                break;
            case LogLevel.Error:
                AdaptableLog.TagError(_tag, formattedMessage);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    private enum LogLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    private static class LogLineFormatter
    {
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            // GameData can reject Json.NET's DynamicMethod-based getters; keep log context conversion on reflection.
            ContractResolver = new ReflectionOnlyContractResolver(),
            Converters =
            {
                new StringEnumConverter(),
            },
            Culture = CultureInfo.InvariantCulture,
            Formatting = Formatting.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public static string Format(
            string message,
            Exception? exception,
            object? context)
        {
            string normalizedMessage = string.IsNullOrWhiteSpace(message)
                ? "(no message)"
                : message.Trim();

            if (context is null && exception is null)
            {
                return normalizedMessage;
            }

            JObject logObject = context is null
                ? []
                : ToLogObject(context);

            if (exception is not null)
            {
                logObject["exceptionType"] = exception.GetType().FullName;
                logObject["exceptionMessage"] = exception.Message;
                logObject["exception"] = exception.ToString();
            }

            return normalizedMessage
                + " | "
                + logObject.ToString(Formatting.None, []);
        }

        private static JObject ToLogObject(object context)
        {
            JToken token = JToken.FromObject(
                context,
                JsonSerializer.Create(JsonSettings));

            if (token is JObject logObject)
            {
                return logObject;
            }

            throw new ArgumentException(
                "Log context must serialize to a JSON object. Use new { field = value }.",
                nameof(context));
        }

        private sealed class ReflectionOnlyContractResolver : DefaultContractResolver
        {
            protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
            {
                return new ReflectionValueProvider(member);
            }
        }
    }
}
