using Lumina.Text.ReadOnly;

namespace AllaganKillFeed;

internal record Notification(TimeSpan Duration, ReadOnlySeString Content, string Title)
{
    public ActiveNotification ToActiveNotification
    {
        get
        {
            var createdAt = DateTime.Now;
            return new ActiveNotification(createdAt, createdAt + Duration, Duration, Content, Title);
        }
    }
}