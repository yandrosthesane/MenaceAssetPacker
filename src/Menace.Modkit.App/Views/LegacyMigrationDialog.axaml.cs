using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Menace.Modkit.App.Services;
using Menace.Modkit.App.Styles;

namespace Menace.Modkit.App.Views;

/// <summary>
/// Dialog for handling legacy installation detection and migration.
/// Displayed when InstallHealthService detects a legacy install pattern.
/// </summary>
public partial class LegacyMigrationDialog : UserControl
{
    private TaskCompletionSource<bool>? _tcs;
    private LegacyInstallDetector.LegacyDetectionResult? _detectionResult;
    private string? _gamePath;
    private bool _operationSucceeded;

    public LegacyMigrationDialog()
    {
        InitializeComponent();

        MigrateButton.Click += OnMigrateClicked;
        CleanResetButton.Click += OnCleanResetClicked;
        ContinueButton.Click += OnContinueClicked;
    }

    /// <summary>
    /// Configure the dialog with detection results.
    /// </summary>
    /// <param name="detectionResult">The legacy detection result.</param>
    /// <param name="gamePath">The game installation path.</param>
    public void Configure(LegacyInstallDetector.LegacyDetectionResult detectionResult, string gamePath)
    {
        _detectionResult = detectionResult;
        _gamePath = gamePath;

        // Update confidence indicator
        var confidence = detectionResult.ConfidenceScore;
        UpdateConfidenceDisplay(confidence);

        // Populate issues list
        IssuesList.ItemsSource = detectionResult.DetectedIssues;

        // Show/hide issues panel based on whether there are issues
        IssuesPanel.IsVisible = detectionResult.DetectedIssues.Count > 0;
    }

    private void UpdateConfidenceDisplay(float confidence)
    {
        // Use theme-consistent colors: teal for success, maroon for errors
        if (confidence >= 0.7f)
        {
            // High confidence - migration recommended (dark teal tint)
            ConfidenceIcon.Text = "\u2713"; // Checkmark
            ConfidenceIcon.Foreground = ThemeColors.BrushPrimaryLight; // Light teal
            ConfidenceText.Text = "High Confidence";
            ConfidenceText.Foreground = ThemeColors.BrushPrimaryLight;
            ConfidenceDescription.Text = "The issues detected are well understood and can likely be fixed automatically. Migration is recommended.";
            ConfidenceBorder.Background = new SolidColorBrush(Color.Parse("#0a1f1c")); // Very dark teal
        }
        else if (confidence >= 0.5f)
        {
            // Medium confidence - either option works
            ConfidenceIcon.Text = "\u26A0"; // Warning
            ConfidenceIcon.Foreground = ThemeColors.BrushTextSecondary;
            ConfidenceText.Text = "Medium Confidence";
            ConfidenceText.Foreground = Brushes.White;
            ConfidenceDescription.Text = "Some issues were detected that may require manual review. Both migration and clean reset are viable options.";
            ConfidenceBorder.Background = ThemeColors.BrushBgSurfaceAlt;
        }
        else
        {
            // Low confidence - clean reset recommended (maroon tint)
            ConfidenceIcon.Text = "\u2717"; // X
            ConfidenceIcon.Foreground = ThemeColors.BrushTextTertiary;
            ConfidenceText.Text = "Low Confidence - Clean Reset Recommended";
            ConfidenceText.Foreground = Brushes.White;
            ConfidenceDescription.Text = "Multiple issues detected that may not migrate cleanly. A clean reset is strongly recommended to avoid future problems.";
            ConfidenceBorder.Background = new SolidColorBrush(Color.Parse("#1a0a0c")); // Very dark maroon

            // Swap button emphasis
            MigrateButton.Classes.Remove("primary");
            MigrateButton.Classes.Add("secondary");
            CleanResetButton.Classes.Remove("destructive");
            CleanResetButton.Classes.Add("primary");
        }
    }

    /// <summary>
    /// Show the dialog and wait for user action.
    /// </summary>
    /// <returns>True if user completed an action, false if cancelled.</returns>
    public Task<bool> ShowAsync()
    {
        _tcs = new TaskCompletionSource<bool>();
        return _tcs.Task;
    }

    private async void OnMigrateClicked(object? sender, RoutedEventArgs e)
    {
        if (_detectionResult == null || _gamePath == null)
            return;

        await PerformOperationAsync(async progress =>
        {
            return await LegacyMigrationService.Instance.MigrateExistingAsync(
                _gamePath, _detectionResult, progress);
        });
    }

    private async void OnCleanResetClicked(object? sender, RoutedEventArgs e)
    {
        if (_gamePath == null)
            return;

        await PerformOperationAsync(async progress =>
        {
            return await LegacyMigrationService.Instance.CleanResetAsync(_gamePath, progress);
        });
    }

    private async Task PerformOperationAsync(
        Func<IProgress<string>, Task<LegacyMigrationService.MigrationResult>> operation)
    {
        // Hide action buttons, show progress
        ButtonPanel.IsVisible = false;
        IssuesPanel.IsVisible = false;
        ConfidenceBorder.IsVisible = false;
        ProgressPanel.IsVisible = true;
        ResultPanel.IsVisible = false;

        var progress = new Progress<string>(message =>
        {
            ProgressText.Text = message;
        });

        try
        {
            var result = await operation(progress);

            // Show result
            ProgressPanel.IsVisible = false;
            ResultPanel.IsVisible = true;

            _operationSucceeded = result.Success;

            // Use theme-consistent colors: teal for success, maroon tint for errors
            if (result.Success)
            {
                ResultPanel.Background = new SolidColorBrush(Color.Parse("#0a1f1c")); // Dark teal
                ResultIcon.Text = "\u2713"; // Checkmark
                ResultIcon.Foreground = ThemeColors.BrushPrimaryLight; // Light teal
                ResultMessage.Text = result.Message;
                ResultMessage.Foreground = ThemeColors.BrushPrimaryLight;
            }
            else
            {
                ResultPanel.Background = new SolidColorBrush(Color.Parse("#1a0a0c")); // Dark maroon
                ResultIcon.Text = "\u2717"; // X
                ResultIcon.Foreground = ThemeColors.BrushTextTertiary;
                ResultMessage.Text = result.Message;
                ResultMessage.Foreground = Brushes.White;
            }

            ResultDetails.ItemsSource = result.Details;

            // Show continue button
            ContinueButton.IsVisible = true;
        }
        catch (Exception ex)
        {
            // Show error - operation failed (use maroon theme)
            _operationSucceeded = false;
            ProgressPanel.IsVisible = false;
            ResultPanel.IsVisible = true;
            ResultPanel.Background = new SolidColorBrush(Color.Parse("#1a0a0c")); // Dark maroon
            ResultIcon.Text = "\u2717";
            ResultIcon.Foreground = ThemeColors.BrushTextTertiary;
            ResultMessage.Text = $"Error: {ex.Message}";
            ResultMessage.Foreground = Brushes.White;
            ResultDetails.ItemsSource = new List<string>();

            ContinueButton.IsVisible = true;
        }
    }

    private void OnContinueClicked(object? sender, RoutedEventArgs e)
    {
        // Only report as handled if the operation actually succeeded
        _tcs?.TrySetResult(_operationSucceeded);
        if (this.Parent is Window window)
            window.Close();
    }

    /// <summary>
    /// Show a legacy migration dialog and wait for user response.
    /// </summary>
    /// <param name="parent">Parent window to center the dialog over.</param>
    /// <param name="detectionResult">The legacy detection result.</param>
    /// <param name="gamePath">The game installation path.</param>
    /// <returns>True if user completed migration/reset, false if cancelled.</returns>
    public static async Task<bool> ShowAsync(
        Window parent,
        LegacyInstallDetector.LegacyDetectionResult detectionResult,
        string gamePath)
    {
        var dialog = new LegacyMigrationDialog();
        dialog.Configure(detectionResult, gamePath);

        var window = new Window
        {
            Content = dialog,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.BorderOnly,
            Background = Brushes.Transparent,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            Title = "Legacy Installation Detected"
        };

        // Set app icon if available
        try
        {
            var iconUri = new Uri("avares://Menace.Modkit.App/Assets/icon.jpg");
            window.Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(iconUri));
        }
        catch { /* Icon loading failed, continue without */ }

        var showTask = dialog.ShowAsync();
        await window.ShowDialog(parent);

        // If the window was closed without completing, treat as false
        if (!showTask.IsCompleted)
            dialog._tcs?.TrySetResult(false);

        return await showTask;
    }
}
