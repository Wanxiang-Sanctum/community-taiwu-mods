namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

/// <summary>
/// 描述嫁接动作成功后推送的一条即时通知。
/// </summary>
/// <param name="templateId">游戏内置即时通知模板 ID，用于决定图标、通知类型和重要程度。</param>
/// <param name="message">玩家可见通知文本。</param>
/// <exception cref="ArgumentException"><paramref name="message"/> 为 null、空字符串或空白字符串。</exception>
public sealed class GraftSuccessNotification(short templateId, string message)
{
    /// <summary>
    /// 获取游戏内置即时通知模板 ID。
    /// </summary>
    public short TemplateId { get; } = templateId;

    /// <summary>
    /// 获取玩家可见通知文本。
    /// </summary>
    public string Message { get; } = ValidateRequiredText(message, nameof(message));

    private static string ValidateRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim();
    }
}
