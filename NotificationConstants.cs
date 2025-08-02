using System.Numerics;
using Dalamud.Interface.Utility;
// ReSharper disable MemberCanBePrivate.Global

namespace AllaganKillFeed;

internal static class NotificationConstants
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
    public const uint BodyTextColor = 0xFF8080FF;
}