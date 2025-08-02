using System.Collections.Concurrent;

namespace AllaganKillFeed;

internal static class NotificationManager
{
    public static readonly ConcurrentBag<ActiveNotification> PendingNotifications = [];
    public static readonly List<ActiveNotification> ActiveNotifications = [];
}