using Dalamud.Interface.Utility;
using System.Numerics;

namespace AllaganKillFeed;

internal static class NotificationDrawer
{
    private static unsafe float CalculateNotificationWidth()
    {
        var notificationWidthMeasurementString = "The width of this text will decide the width\\nof the notification window."u8;
        var viewportSize = ImGuiHelpers.MainViewport.WorkSize;
        Vector2 notificationSize;
        fixed (byte* ptr = notificationWidthMeasurementString)
            ImGuiNative.CalcTextSize(&notificationSize, ptr, ptr + notificationWidthMeasurementString.Length, 0, -1);
        var width = notificationSize.X;
        width += NotificationConstants.ScaledWindowPadding * 3;
        return Math.Min(width, viewportSize.X * NotificationConstants.MaxNotificationWindowWidthWrtMainViewportWidth);
    }

    public static void Draw()
    {
        var height = 0f;
        var width = CalculateNotificationWidth();

        while (NotificationManager.PendingNotifications.TryTake(out var notification))
            NotificationManager.ActiveNotifications.Add(notification);

        List<int> toRemove = [];
        for (var i = 0; i < NotificationManager.ActiveNotifications.Count; i++)
        {
            var notification = NotificationManager.ActiveNotifications[i];
            if (notification.Expiry < DateTime.Now)
                toRemove.Add(i);
            else
            {
                height += notification.Draw(width, height);
                height += NotificationConstants.ScaledWindowGap;
            }
        }

        for (var i = toRemove.Count - 1; i >= 0; i--)
            NotificationManager.ActiveNotifications.RemoveAt(toRemove[i]);
    }
}