using Config;
using FrameWork;
using GameData.Domains.LifeRecord.GeneralRecord;
using GameData.Domains.World.Notification;
using UILogic.DisplayDataStructure;

namespace Wanxiang.Taiwu.InstantNotifications;

/// <summary>
/// 推送调用方指定文本的太吾前端即时通知。
/// </summary>
public static class InstantNotificationPublisher
{
    private const string NewMessageIndexKey = "NewMessageIndex";

    /// <summary>
    /// 把消息写入游戏前端即时通知列表，并触发新通知 UI 事件。
    /// </summary>
    /// <param name="templateId">游戏内置即时通知模板 ID，用于决定图标、通知类型和重要程度。</param>
    /// <param name="message">玩家可见通知文本。</param>
    /// <exception cref="ArgumentException"><paramref name="message"/> 为 null、空字符串或空白字符串。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="templateId"/> 不存在于游戏即时通知配置。</exception>
    public static void Push(
        short templateId,
        string message)
    {
        string normalizedMessage = ValidateRequiredText(message, nameof(message));
        ValidateTemplateId(templateId, nameof(templateId));

        DisplayTriggerModel displayTriggerModel = SingletonObject.getInstance<DisplayTriggerModel>();
        displayTriggerModel.RenderedNotificationList ??= [];

        int newMessageIndex = displayTriggerModel.RenderedNotificationList.Count;
        int currentDate = SingletonObject.getInstance<BasicGameData>().CurrDate;
        InstantNotificationRenderInfo renderInfo = new(
            templateId,
            normalizedMessage,
            normalizedMessage,
            currentDate);

        NotificationItem notification = new(currentDate, renderInfo, static _ => null)
        {
            ReadState = false,
            RenderedArgumentCollection = new RenderedArgumentCollection(),
        };

        displayTriggerModel.RenderedNotificationList.Add(notification);
        GEvent.OnEvent(
            UiEvents.OnNewInstantNotification,
            EasyPool.Get<ArgumentBox>().Set(NewMessageIndexKey, newMessageIndex));
    }

    private static void ValidateTemplateId(
        short templateId,
        string parameterName)
    {
        if (InstantNotification.Instance.GetItem(templateId) is null)
        {
            throw new ArgumentOutOfRangeException(parameterName, templateId, "Native instant notification template does not exist.");
        }
    }

    private static string ValidateRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim();
    }
}
