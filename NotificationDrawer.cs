using Dalamud.Interface.Utility;
using ImGuiNET;
using Lumina.Text.ReadOnly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Dalamud.Interface.Colors;

namespace AllaganKillFeed;

internal class NotificationConstants
{
    public static float ScaledWindowPadding => MathF.Round(16 * ImGuiHelpers.GlobalScale);
    public static float ScaledViewportEdgeMargin => MathF.Round(20 * ImGuiHelpers.GlobalScale);
    public static float ScaledIconSize => MathF.Round(IconSize * ImGuiHelpers.GlobalScale);
    public static float ScaledCoponentGap => MathF.Round(2 * ImGuiHelpers.GlobalScale);
    public static float ScaledExpiryProgressBarHeight => MathF.Round(3 * ImGuiHelpers.GlobalScale);
    public static float ScaledWindowGap => MathF.Round(10 * ImGuiHelpers.GlobalScale);
    public const float MaxNotificationWindowWidthWrtMainViewportWidth = 2f / 3;
    public const float ProgressWaveLoopDuration = 2000f;
    public const float ProgressWaveIdleTimeRatio = .5f;
    public const float ProgressWaveLoopMaxColorTimeRatio = .7f;
    public const float IconSize = 32;
    public static Vector4 TitleTextColor = new(1, 1, 1, 1);
    public static Vector4 BackgroundProgressColorMin = new(1, 1, 1, .05f);
    public static Vector4 BackgroundProgressColorMax = new(1, 1, 1, .1f);
    public static Vector4 BlameTextColor = new(.8f,.8f, .8f, 1f);
    public static Vector4 BodyTextColor = new(.9f, .9f, .9f, 1f);
}

internal class NotificationDrawer
{
    private static unsafe float CalculateNotificationWidth()
    {
        var notificationWidthMeasurementString = "The width of this text will decide the width\\nof the notification window."u8;
        var viewportSize = ImGuiHelpers.MainViewport.WorkSize;
        Vector2 notificationSize;
        fixed (byte* ptr = notificationWidthMeasurementString)
            ImGuiNative.igCalcTextSize(&notificationSize, ptr, ptr + notificationWidthMeasurementString.Length, 0, -1);
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

        List<int> toRemove = new List<int>();
        for (int i = 0; i < NotificationManager.ActiveNotifications.Count; i++)
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

        for (int i = toRemove.Count - 1; i >= 0; i--)
            NotificationManager.ActiveNotifications.RemoveAt(toRemove[i]);
    }
}

internal class NotificationManager
{
    public static ConcurrentBag<ActiveNotification> PendingNotifications = [];
    public static List<ActiveNotification> ActiveNotifications = [];
}

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

internal record ActiveNotification(DateTime CreatedAt, DateTime Expiry, TimeSpan Duration, ReadOnlySeString Content, string Title)
{
    private static long _idCounter;
    private long id = Interlocked.Increment(ref _idCounter);

    public float Draw(float width, float height)
    {
        var actionWindowHeight = ImGui.GetTextLineHeight() + NotificationConstants.ScaledWindowPadding * 2;
        var viewport = ImGuiHelpers.MainViewport;
        var viewportSize = viewport.WorkSize;
        var viewportPos = viewport.Pos;

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.8f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(NotificationConstants.ScaledWindowPadding));
        unsafe
        {
            ImGui.PushStyleColor(ImGuiCol.WindowBg,
                *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg) *
                new Vector4(1, 1, 1, 0.82f));
        }

        var xPos = viewportSize.X - width * 1f;
        xPos = Math.Min(viewportSize.X - width - NotificationConstants.ScaledViewportEdgeMargin, xPos);
        var yPos = viewportSize.Y - height - NotificationConstants.ScaledViewportEdgeMargin;
        var topLeft = new Vector2(xPos, yPos);
        var pivot = new Vector2(0, 1);

        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGui.SetNextWindowPos(topLeft + viewportPos, ImGuiCond.Always, pivot);
        var size = new Vector2(width, actionWindowHeight);
        ImGui.SetNextWindowSizeConstraints(size, size);
        ImGui.Begin($"##AllaganKillFeedNotification{id}",
            ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoInputs |
            ImGuiWindowFlags.NoSavedSettings);
        ImGui.PushID(id.GetHashCode());
        DrawWindowBackgroundProgressBar();
        var textColumnWidth = width + NotificationConstants.ScaledWindowPadding;
        var textColumnOffset = new Vector2(NotificationConstants.ScaledWindowPadding, NotificationConstants.ScaledCoponentGap);
        textColumnOffset.Y += DrawTitle(textColumnOffset, textColumnWidth);
        textColumnOffset.Y += NotificationConstants.ScaledCoponentGap;
        DrawContentBody(textColumnOffset, textColumnWidth);
        DrawExpiryBar();
        var windowSize = ImGui.GetWindowSize();
        ImGui.PopID();
        ImGui.End();

        ImGui.PopStyleColor();
        ImGui.PopStyleVar(3);
        return windowSize.Y;
    }

    private void DrawExpiryBar()
    {
        var barL = 1 - (float)((Expiry - DateTime.Now).TotalMilliseconds / Duration.TotalMilliseconds);
        var barR = 1;
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        ImGui.PushClipRect(windowPos, windowPos + windowSize, false);
        ImGui.GetWindowDrawList().AddRectFilled(
            windowPos + new Vector2(
                windowSize.X * barL,
                windowSize.Y - NotificationConstants.ScaledExpiryProgressBarHeight),
            windowPos + windowSize with { X = windowSize.X * barR },
            ImGui.GetColorU32(ImGuiColors.DalamudWhite));
        ImGui.PopClipRect();
    }

    private void DrawContentBody(Vector2 minCoord, float width)
    {
        ImGui.SetCursorPos(minCoord);
        ImGui.PushTextWrapPos(minCoord.X + width);
        ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.BodyTextColor);
        ImGuiHelpers.SeStringWrapped(Content);
        ImGui.PopStyleColor();
        ImGui.PopTextWrapPos();
    }

    private float DrawTitle(Vector2 minCoord, float width)
    {
        ImGui.PushTextWrapPos(minCoord.X + width);

        ImGui.SetCursorPos(minCoord);
        ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.TitleTextColor);
        ImGui.TextUnformatted(Title);
        ImGui.PopStyleColor();

        ImGui.PopTextWrapPos();
        return ImGui.GetCursorPosY() - minCoord.Y;
    }

    private void DrawWindowBackgroundProgressBar()
    {
        var elapsed = 0f;
        var colorElapsed = 0f;
        float progress;

        progress = Math.Clamp((float)((DateTime.Now - CreatedAt).TotalMilliseconds / Duration.TotalMilliseconds), 0f, 1f);

        elapsed =
            (float)(((DateTime.Now - CreatedAt).TotalMilliseconds %
                     NotificationConstants.ProgressWaveLoopDuration) /
                    NotificationConstants.ProgressWaveLoopDuration);
        elapsed /= NotificationConstants.ProgressWaveIdleTimeRatio;

        colorElapsed = elapsed < NotificationConstants.ProgressWaveLoopMaxColorTimeRatio
                           ? elapsed / NotificationConstants.ProgressWaveLoopMaxColorTimeRatio
                           : ((NotificationConstants.ProgressWaveLoopMaxColorTimeRatio * 2) - elapsed) /
                             NotificationConstants.ProgressWaveLoopMaxColorTimeRatio;

        elapsed = Math.Clamp(elapsed, 0f, 1f);
        colorElapsed = Math.Clamp(colorElapsed, 0f, 1f);
        colorElapsed = MathF.Sin(colorElapsed * (MathF.PI / 2f));

        if (progress >= 1f)
            elapsed = colorElapsed = 0f;

        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var rb = windowPos + windowSize;
        var midp = windowPos + windowSize with { X = windowSize.X * progress * elapsed };
        var rp = windowPos + windowSize with { X = windowSize.X * progress };

        ImGui.PushClipRect(windowPos, rb, false);
        ImGui.GetWindowDrawList().AddRectFilled(
            windowPos,
            midp,
            ImGui.GetColorU32(
                Vector4.Lerp(
                    NotificationConstants.BackgroundProgressColorMin,
                    NotificationConstants.BackgroundProgressColorMax,
                    colorElapsed)));
        ImGui.GetWindowDrawList().AddRectFilled(
            midp with { Y = 0 },
            rp,
            ImGui.GetColorU32(NotificationConstants.BackgroundProgressColorMin));
        ImGui.PopClipRect();
    }
}