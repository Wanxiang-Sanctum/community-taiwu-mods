using Config;
using FrameWork;
using GameData.Domains.LifeRecord.GeneralRecord;
using GameData.Domains.World.Notification;
using UILogic.DisplayDataStructure;

namespace Wanxiang.Taiwu.ItemGrafts.Frontend;

internal static class GraftNotifications
{
    internal const short DefaultNativeRecordType = 254;

    private const string NewMessageIndexKey = "NewMessageIndex";

    internal static void Push(
        string message,
        short nativeRecordType = DefaultNativeRecordType)
    {
        string normalizedMessage = ValidateRequiredText(message, nameof(message));
        ValidateNativeRecordType(nativeRecordType, nameof(nativeRecordType));

        DisplayTriggerModel displayTriggerModel = SingletonObject.getInstance<DisplayTriggerModel>();
        displayTriggerModel.RenderedNotificationList ??= [];

        int newMessageIndex = displayTriggerModel.RenderedNotificationList.Count;
        int currentDate = SingletonObject.getInstance<BasicGameData>().CurrDate;
        InstantNotificationRenderInfo renderInfo = new(
            nativeRecordType,
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

    private static void ValidateNativeRecordType(
        short nativeRecordType,
        string parameterName)
    {
        InstantNotificationItem item = InstantNotification.Instance.GetItem(nativeRecordType)
            ?? throw new ArgumentOutOfRangeException(parameterName, nativeRecordType, "Native instant notification record type does not exist.");

        if (item.Type >= EInstantNotificationType.Count)
        {
            throw new ArgumentOutOfRangeException(parameterName, nativeRecordType, "Native instant notification record type has an invalid display type.");
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
