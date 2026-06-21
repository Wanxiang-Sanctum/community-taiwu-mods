using System.Globalization;
using System.Reflection;
using GameData.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Wanxiang.Taiwu.Logging;

/// <summary>
/// 使用固定日志标签向太吾游戏日志写入紧凑的结构化消息。
/// </summary>
public sealed class TaiwuLogger
{
    private readonly string _tag;

    private TaiwuLogger(string tag)
    {
        _tag = ValidateTag(tag);
    }

    /// <summary>
    /// 创建使用指定太吾日志标签写入消息的日志记录器。
    /// </summary>
    /// <param name="tag">非空日志标签。</param>
    /// <returns>绑定到该标签的日志记录器。</returns>
    public static TaiwuLogger ForTag(string tag)
    {
        return new TaiwuLogger(tag);
    }

    /// <summary>
    /// 写入信息级日志消息。
    /// </summary>
    /// <param name="message">消息文本。</param>
    /// <param name="context">可选结构化上下文，必须能序列化为 JSON 对象。</param>
    public void Info(string message, object? context = null)
    {
        Write(LogLevel.Info, exception: null, message, context);
    }

    /// <summary>
    /// 写入警告级日志消息。
    /// </summary>
    /// <param name="message">消息文本。</param>
    /// <param name="context">可选结构化上下文，必须能序列化为 JSON 对象。</param>
    public void Warning(string message, object? context = null)
    {
        Write(LogLevel.Warning, exception: null, message, context);
    }

    /// <summary>
    /// 写入错误级日志消息。
    /// </summary>
    /// <param name="message">消息文本。</param>
    /// <param name="context">可选结构化上下文，必须能序列化为 JSON 对象。</param>
    public void Error(string message, object? context = null)
    {
        Write(LogLevel.Error, exception: null, message, context);
    }

    /// <summary>
    /// 写入包含异常详情的错误级日志消息。
    /// </summary>
    /// <param name="exception">要写入结构化日志上下文的异常。</param>
    /// <param name="message">消息文本。</param>
    /// <param name="context">可选结构化上下文，必须能序列化为 JSON 对象。</param>
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
            // GameData 可能拒绝 Json.NET 基于 DynamicMethod 的 getter；日志上下文转换保持使用反射。
            ContractResolver = new ReflectionOnlyContractResolver(),
            Converters =
            {
                new StringEnumConverter(),
            },
            Culture = CultureInfo.InvariantCulture,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
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
