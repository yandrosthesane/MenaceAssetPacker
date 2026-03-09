using Avalonia;
using Avalonia.Media;

namespace Menace.Modkit.App.Styles;

/// <summary>
/// Centralized theme colors for use in C# code-behind.
/// These values should match Theme.axaml definitions.
/// Use the Brush properties for direct use in UI, or Parse methods for dynamic scenarios.
/// </summary>
public static class ThemeColors
{
    // ═══════════════════════════════════════════════════════════════════
    // PRIMARY BRAND - Dark Teal
    // ═══════════════════════════════════════════════════════════════════
    public static readonly Color Primary = Color.Parse("#004f43");
    public static readonly Color PrimaryLight = Color.Parse("#8ECDC8");
    public static readonly Color PrimaryMuted = Color.Parse("#003d34");
    public static readonly Color PrimaryHover = Color.Parse("#006657");

    public static readonly IBrush BrushPrimary = new SolidColorBrush(Primary);
    public static readonly IBrush BrushPrimaryLight = new SolidColorBrush(PrimaryLight);
    public static readonly IBrush BrushPrimaryMuted = new SolidColorBrush(PrimaryMuted);
    public static readonly IBrush BrushPrimaryHover = new SolidColorBrush(PrimaryHover);

    // ═══════════════════════════════════════════════════════════════════
    // SECONDARY BRAND - Maroon
    // ═══════════════════════════════════════════════════════════════════
    public static readonly Color Maroon = Color.Parse("#410511");
    public static readonly Color MaroonHover = Color.Parse("#5a0717");

    public static readonly IBrush BrushMaroon = new SolidColorBrush(Maroon);
    public static readonly IBrush BrushMaroonHover = new SolidColorBrush(MaroonHover);

    // ═══════════════════════════════════════════════════════════════════
    // SEMANTIC COLORS
    // ═══════════════════════════════════════════════════════════════════
    public static readonly Color Success = Color.Parse("#22c55e");
    public static readonly Color SuccessBg = Color.Parse("#1a3a2a");
    public static readonly Color Warning = Color.Parse("#c89b3c");
    public static readonly Color WarningBg = Color.Parse("#3d2e00");
    public static readonly Color WarningLight = Color.Parse("#F59E0B");
    public static readonly Color Error = Color.Parse("#ef4444");
    public static readonly Color ErrorMuted = Color.Parse("#FF8888");
    public static readonly Color ErrorBg = Color.Parse("#3d1a1a");
    public static readonly Color ErrorDark = Color.Parse("#991b1b");
    public static readonly Color Info = Color.Parse("#8ECDC8");
    public static readonly Color InfoBg = Color.Parse("#1a2a3a");

    public static readonly IBrush BrushSuccess = new SolidColorBrush(Success);
    public static readonly IBrush BrushSuccessBg = new SolidColorBrush(SuccessBg);
    public static readonly IBrush BrushWarning = new SolidColorBrush(Warning);
    public static readonly IBrush BrushWarningBg = new SolidColorBrush(WarningBg);
    public static readonly IBrush BrushWarningLight = new SolidColorBrush(WarningLight);
    public static readonly IBrush BrushError = new SolidColorBrush(Error);
    public static readonly IBrush BrushErrorMuted = new SolidColorBrush(ErrorMuted);
    public static readonly IBrush BrushErrorBg = new SolidColorBrush(ErrorBg);
    public static readonly IBrush BrushErrorDark = new SolidColorBrush(ErrorDark);
    public static readonly IBrush BrushInfo = new SolidColorBrush(Info);
    public static readonly IBrush BrushInfoBg = new SolidColorBrush(InfoBg);

    // Status indicator colors (for icons in dialogs/status displays)
    public static readonly Color StatusSuccess = Color.Parse("#4EC9B0");
    public static readonly Color StatusWarning = Color.Parse("#FFB347");
    public static readonly Color StatusError = Color.Parse("#FF6B6B");

    public static readonly IBrush BrushStatusSuccess = new SolidColorBrush(StatusSuccess);
    public static readonly IBrush BrushStatusWarning = new SolidColorBrush(StatusWarning);
    public static readonly IBrush BrushStatusError = new SolidColorBrush(StatusError);

    // ═══════════════════════════════════════════════════════════════════
    // ACCENT COLORS (desaturated to blend with grey palette)
    // ═══════════════════════════════════════════════════════════════════
    public static readonly Color Gold = Color.Parse("#4c4937");
    public static readonly Color TealActive = Color.Parse("#0d9488");
    public static readonly Color Purple = Color.Parse("#403859");

    public static readonly IBrush BrushGold = new SolidColorBrush(Gold);
    public static readonly IBrush BrushTealActive = new SolidColorBrush(TealActive);
    public static readonly IBrush BrushPurple = new SolidColorBrush(Purple);

    // ═══════════════════════════════════════════════════════════════════
    // BACKGROUNDS
    // ═══════════════════════════════════════════════════════════════════
    public static readonly Color BgBase = Color.Parse("#0A0A0A");
    public static readonly Color BgDark = Color.Parse("#0D0D0D");
    public static readonly Color BgDarker = Color.Parse("#0F0F0F");
    public static readonly Color BgSurface = Color.Parse("#1A1A1A");
    public static readonly Color BgSurfaceAlt = Color.Parse("#1E1E1E");
    public static readonly Color BgElevated = Color.Parse("#252525");
    public static readonly Color BgInput = Color.Parse("#2A2A2A");
    public static readonly Color BgHover = Color.Parse("#333333");
    public static readonly Color BgWindow = Color.Parse("#121212");
    public static readonly Color BgPanelLeft = Color.Parse("#141414");
    public static readonly Color BgPanelRight = Color.Parse("#1A1A1A");

    public static readonly IBrush BrushBgBase = new SolidColorBrush(BgBase);
    public static readonly IBrush BrushBgDark = new SolidColorBrush(BgDark);
    public static readonly IBrush BrushBgDarker = new SolidColorBrush(BgDarker);
    public static readonly IBrush BrushBgSurface = new SolidColorBrush(BgSurface);
    public static readonly IBrush BrushBgSurfaceAlt = new SolidColorBrush(BgSurfaceAlt);
    public static readonly IBrush BrushBgElevated = new SolidColorBrush(BgElevated);
    public static readonly IBrush BrushBgInput = new SolidColorBrush(BgInput);
    public static readonly IBrush BrushBgHover = new SolidColorBrush(BgHover);
    public static readonly IBrush BrushBgWindow = new SolidColorBrush(BgWindow);
    public static readonly IBrush BrushBgPanelLeft = new SolidColorBrush(BgPanelLeft);
    public static readonly IBrush BrushBgPanelRight = new SolidColorBrush(BgPanelRight);

    // ═══════════════════════════════════════════════════════════════════
    // BORDERS
    // ═══════════════════════════════════════════════════════════════════
    public static readonly Color Border = Color.Parse("#2D2D2D");
    public static readonly Color BorderLight = Color.Parse("#3E3E3E");
    public static readonly Color BorderPrimary = Color.Parse("#004f43");

    public static readonly IBrush BrushBorder = new SolidColorBrush(Border);
    public static readonly IBrush BrushBorderLight = new SolidColorBrush(BorderLight);
    public static readonly IBrush BrushBorderPrimary = new SolidColorBrush(BorderPrimary);

    // ═══════════════════════════════════════════════════════════════════
    // TEXT
    // ═══════════════════════════════════════════════════════════════════
    public static readonly Color TextPrimary = Color.Parse("#FFFFFF");
    public static readonly Color TextSecondary = Color.Parse("#AAAAAA");
    public static readonly Color TextTertiary = Color.Parse("#888888");
    public static readonly Color TextMuted = Color.Parse("#666666");
    public static readonly Color TextDim = Color.Parse("#999999");
    public static readonly Color TextLink = Color.Parse("#6DB3F2");

    public static readonly IBrush BrushTextPrimary = new SolidColorBrush(TextPrimary);
    public static readonly IBrush BrushTextSecondary = new SolidColorBrush(TextSecondary);
    public static readonly IBrush BrushTextTertiary = new SolidColorBrush(TextTertiary);
    public static readonly IBrush BrushTextMuted = new SolidColorBrush(TextMuted);
    public static readonly IBrush BrushTextDim = new SolidColorBrush(TextDim);
    public static readonly IBrush BrushTextLink = new SolidColorBrush(TextLink);

    // ═══════════════════════════════════════════════════════════════════
    // ICONS
    // ═══════════════════════════════════════════════════════════════════
    public static readonly Color IconDefault = Color.Parse("#AAAAAA");
    public static readonly Color IconFunction = Color.Parse("#3B7DD8");
    public static readonly Color IconEvent = Color.Parse("#8B5CF6");
    public static readonly Color IconInterceptor = Color.Parse("#004f43");

    public static readonly IBrush BrushIconDefault = new SolidColorBrush(IconDefault);
    public static readonly IBrush BrushIconFunction = new SolidColorBrush(IconFunction);
    public static readonly IBrush BrushIconEvent = new SolidColorBrush(IconEvent);
    public static readonly IBrush BrushIconInterceptor = new SolidColorBrush(IconInterceptor);

    // ═══════════════════════════════════════════════════════════════════
    // CODE EDITOR
    // ═══════════════════════════════════════════════════════════════════
    public static readonly Color CodeForeground = Color.Parse("#D4D4D4");
    public static readonly Color CodeIdentifier = Color.Parse("#9CDCFE");

    public static readonly IBrush BrushCodeForeground = new SolidColorBrush(CodeForeground);
    public static readonly IBrush BrushCodeIdentifier = new SolidColorBrush(CodeIdentifier);
}

/// <summary>
/// Centralized unicode icons for use in C# code-behind.
/// These values should match Theme.axaml definitions.
/// </summary>
public static class ThemeIcons
{
    // Status indicators
    public const string Checkmark = "\u2713";      // ✓
    public const string CheckmarkHeavy = "\u2714"; // ✔
    public const string Cross = "\u2717";          // ✗
    public const string CrossHeavy = "\u2716";     // ✖
    public const string Warning = "\u26A0";        // ⚠
    public const string NoEntry = "\u26d4";        // ⛔
    public const string Info = "\u2139";           // ℹ

    // Stars (favorites)
    public const string StarFilled = "\u2605";     // ★
    public const string StarEmpty = "\u2606";      // ☆

    // Arrows and navigation
    public const string ArrowUp = "\u25B2";        // ▲
    public const string ArrowDown = "\u25BC";      // ▼
    public const string ArrowUpSmall = "\u2191";   // ↑
    public const string ArrowDownSmall = "\u2193"; // ↓

    // Misc
    public const string Delete = "\u2715";         // ✕
    public const string Menu = "\u2630";           // ☰
    public const string VerticalDots = "\u22EE";   // ⋮
    public const string Ellipsis = "\u2026";       // …
    public const string Refresh = "\u21BB";        // ↻
    public const string Hourglass = "\u23f3";      // ⏳
    public const string Dash = "\u2014";           // —
    public const string Bullet = "\u2022";         // •
}
