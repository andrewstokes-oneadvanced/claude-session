using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ClaudeCodeHelper
{
    public class TrayApplicationContext : ApplicationContext
    {
        // Windows API declarations for window management
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const uint GW_OWNER = 4;
        private const int SW_RESTORE = 9;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_RETURN = 0x0D;

        private NotifyIcon _trayIcon;
        private System.Threading.Timer _commandTimer = null!;
        private System.Threading.Timer _tooltipUpdateTimer = null!;
        private TimeSpan _sessionDuration = TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(1)); // Made non-readonly
        private DateTime _lastSentTime;
        private DateTime _currentSessionStartTime;
        private DateTime _nextScheduledSend; // Next scheduled "hi" send time
        private string _lastResponse = "";
        private bool _lastCommandSuccessful = false;
        private string _claudeCliCommand = "claude";  // Default CLI command
        private string _workingDirectory = "";  // Custom working directory for CLI windows (empty = use default)
        private bool _autoLaunchOnStartup = false;  // Whether to automatically launch Claude CLI on startup
        private string _messageToSend = "hi";  // Configurable message to send to Claude CLI
        private const string APP_NAME = "ClaudeCodeHelper";

        public TrayApplicationContext()
        {
            // Load configuration first
            LoadCliConfiguration();
            LoadLastSentTime();
            LoadSessionData();

            // Initialize tray icon with loaded configuration
            _trayIcon = new NotifyIcon()
            {
                Icon = CreateNinjaIcon(),
                ContextMenuStrip = CreateContextMenu(),
                Visible = true,
                Text = "Claude CLI Session Manager"
            };

            _trayIcon.DoubleClick += OnTrayIconDoubleClick;

            // Set up Windows startup
            SetStartupRegistry();

            // Start the timers
            StartTimer();
            StartTooltipUpdateTimer();

            // Initial tooltip update
            UpdateTooltip();

            // Show startup notification
            string startupMessage = string.IsNullOrEmpty(_claudeCliCommand) ?
                "Claude CLI Session Manager started. Configure your Claude CLI command." :
                "Claude CLI Session Manager started. Will maintain active CLI sessions automatically.";
            
            ToolTipIcon startupIcon = string.IsNullOrEmpty(_claudeCliCommand) ? ToolTipIcon.Warning : ToolTipIcon.Info;
            
            _trayIcon.ShowBalloonTip(4000, "Claude CLI Session Manager", startupMessage, startupIcon);

            // Auto-launch Claude CLI if enabled and no CLI windows are currently running
            if (_autoLaunchOnStartup && !string.IsNullOrEmpty(_claudeCliCommand))
            {
                var existingWindows = FindClaudeCliWindows();
                if (!existingWindows.Any())
                {
                    LaunchClaudeCliWindow();
                }
            }
        }

        private Icon CreateNinjaIcon()
        {
            try
            {
                // Create a 32x32 bitmap for the icon (standard system tray size)
                using (Bitmap bitmap = new Bitmap(32, 32))
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // Clear background to transparent
                    g.Clear(Color.Transparent);
                    
                    // Anti-aliasing for smoother edges
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    
                    // Enhanced colors for better visibility
                    Color darkBlue = Color.FromArgb(255, 25, 25, 112);     // Dark blue for head
                    Color lightBlue = Color.FromArgb(255, 70, 130, 180);  // Steel blue for outline
                    Color brightWhite = Color.FromArgb(255, 255, 255, 255); // Bright white for eyes
                    Color brightRed = Color.FromArgb(255, 255, 69, 0);     // Red accent
                    
                    // Draw main head (larger and more visible)
                    using (SolidBrush headBrush = new SolidBrush(darkBlue))
                    {
                        g.FillEllipse(headBrush, 2, 4, 28, 28);
                    }
                    
                    // Draw outline for better definition
                    using (Pen outlinePen = new Pen(lightBlue, 2))
                    {
                        g.DrawEllipse(outlinePen, 2, 4, 28, 28);
                    }
                    
                    // Draw larger, more visible eyes
                    using (SolidBrush eyeBrush = new SolidBrush(brightWhite))
                    {
                        g.FillEllipse(eyeBrush, 8, 12, 6, 4); // Left eye (larger)
                        g.FillEllipse(eyeBrush, 18, 12, 6, 4); // Right eye (larger)
                    }
                    
                    // Draw a distinctive red accent mark
                    using (SolidBrush accentBrush = new SolidBrush(brightRed))
                    {
                        g.FillEllipse(accentBrush, 14, 8, 4, 4);
                    }
                    
                    // Draw mask band with better contrast
                    using (SolidBrush maskBrush = new SolidBrush(lightBlue))
                    {
                        g.FillRectangle(maskBrush, 4, 14, 24, 6);
                    }
                    
                    // Redraw eye holes for mask effect
                    using (SolidBrush eyeBrush = new SolidBrush(brightWhite))
                    {
                        g.FillEllipse(eyeBrush, 9, 15, 4, 3); // Left eye hole
                        g.FillEllipse(eyeBrush, 19, 15, 4, 3); // Right eye hole
                    }
                    
                    // Create icon from bitmap
                    IntPtr hIcon = bitmap.GetHicon();
                    return Icon.FromHandle(hIcon);
                }
            }
            catch
            {
                // Fallback to system icon if creation fails
                return SystemIcons.Application;
            }
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var launchCliItem = new ToolStripMenuItem("🖥️ Launch New Claude CLI Window");
            launchCliItem.ToolTipText = "Opens a new Command Prompt window with Claude CLI running. Uses your configured CLI command and working directory.";
            launchCliItem.Click += (s, e) => LaunchClaudeCliWindow();
            menu.Items.Add(launchCliItem);

            var sendHiItem = new ToolStripMenuItem("💬 Send Message to all CLI Windows");
            sendHiItem.ToolTipText = "Immediately sends your configured message to all open Claude CLI windows to keep sessions active.";
            sendHiItem.Click += (s, e) => SendHiCommand();
            menu.Items.Add(sendHiItem);

            var lastResponseItem = new ToolStripMenuItem("📄 View Last Response");
            lastResponseItem.ToolTipText = "Shows details about the last message sent, including timestamp and success status.";
            lastResponseItem.Click += (s, e) => ShowLastResponse();
            menu.Items.Add(lastResponseItem);

            menu.Items.Add(new ToolStripSeparator());

            var lastSentItem = new ToolStripMenuItem("📊 Last Sent");
            lastSentItem.Enabled = false;
            lastSentItem.ToolTipText = "Shows when the last message was sent to Claude CLI windows.";
            lastSentItem.Tag = "lastSent";
            UpdateLastSentMenuItem(lastSentItem);
            menu.Items.Add(lastSentItem);

            var nextSendItem = new ToolStripMenuItem("⏱️ Next Send");
            nextSendItem.Enabled = false;
            nextSendItem.ToolTipText = "Shows when the next automatic message will be sent to Claude CLI windows.";
            nextSendItem.Tag = "nextSend";
            UpdateNextSendMenuItem(nextSendItem);
            menu.Items.Add(nextSendItem);

            menu.Items.Add(new ToolStripSeparator());

            // Configuration section
            var cliConfigItem = new ToolStripMenuItem("⚙️ Configure Claude CLI");
            cliConfigItem.ToolTipText = "Set up the Claude CLI command to use when launching new windows. Includes test functionality.";
            cliConfigItem.Click += (s, e) => ConfigureClaudeCli();
            menu.Items.Add(cliConfigItem);

            var messageConfigItem = new ToolStripMenuItem("💭 Configure Message");
            messageConfigItem.ToolTipText = "Customize the message sent to Claude CLI windows. Default is 'hi' but you can use any text.";
            messageConfigItem.Click += (s, e) => ConfigureMessage();
            menu.Items.Add(messageConfigItem);

            var workingDirItem = new ToolStripMenuItem("📁 Configure Working Directory");
            workingDirItem.ToolTipText = "Set the default directory where Claude CLI windows will start. Leave empty to use your user profile directory.";
            workingDirItem.Click += (s, e) => ConfigureWorkingDirectory();
            menu.Items.Add(workingDirItem);

            var scheduleConfigItem = new ToolStripMenuItem("⏰ Configure Schedule");
            scheduleConfigItem.ToolTipText = "Set how often messages are sent and customize specific send times. Default is every 5 hours.";
            scheduleConfigItem.Click += (s, e) => ConfigureSchedule();
            menu.Items.Add(scheduleConfigItem);

            menu.Items.Add(new ToolStripSeparator());

            // Startup behavior section
            var autoLaunchItem = new ToolStripMenuItem("🚀 Auto-launch new CLI on Startup");
            autoLaunchItem.Checked = _autoLaunchOnStartup;
            autoLaunchItem.ToolTipText = "Automatically launches a new Claude CLI window when the application starts. Only applies if no existing Claude CLI windows are found.";
            autoLaunchItem.Click += (s, e) => ToggleAutoLaunchOnStartup(autoLaunchItem);
            menu.Items.Add(autoLaunchItem);

            var startupItem = new ToolStripMenuItem("🔧 Run at Windows Startup");
            startupItem.Checked = IsInStartup();
            startupItem.ToolTipText = "Make this application start automatically when Windows boots up. Useful for always having Claude session management active.";
            startupItem.Click += (s, e) => ToggleStartup(startupItem);
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("❌ Exit");
            exitItem.ToolTipText = "Close the Claude CLI Session Manager completely. All automatic messaging will stop.";
            exitItem.Click += OnExit;
            menu.Items.Add(exitItem);

            return menu;
        }

        private void StartTimer()
        {
            // Calculate when to send the next message
            TimeSpan dueTime = CalculateNextSendTime();
            
            _commandTimer = new System.Threading.Timer(
                callback: _ => {
                    SendHiCommand();
                    // SendHiCommand will automatically schedule the next send using the configured duration
                },
                state: null,
                dueTime: dueTime,
                period: Timeout.InfiniteTimeSpan // Don't repeat automatically
            );
        }

        private void RestartTimer()
        {
            _commandTimer?.Dispose();
            StartTimer();
        }

        private void OnTimerElapsed(object? state)
        {
            SendHiCommand();
            // Note: SendHiCommand will automatically schedule the next send using the configured duration
        }

        private TimeSpan CalculateNextSendTime()
        {
            DateTime now = DateTime.Now;
            
            // If we have a scheduled send time, use it
            if (_nextScheduledSend != DateTime.MinValue)
            {
                if (now >= _nextScheduledSend)
                {
                    return TimeSpan.Zero; // Send immediately
                }
                return _nextScheduledSend - now;
            }
            
            // If we haven't sent anything yet, send immediately
            if (_lastSentTime == DateTime.MinValue)
            {
                return TimeSpan.Zero;
            }

            // Calculate when the current session expires (5 hours from first message)
            DateTime sessionExpiry = _currentSessionStartTime.Add(_sessionDuration);
            
            // If session has already expired, send immediately
            if (now >= sessionExpiry)
            {
                return TimeSpan.Zero;
            }
            
            // Otherwise, wait until session expiry
            return sessionExpiry - now;
        }

        private void StartTooltipUpdateTimer()
        {
            // Update tooltip every 5 seconds
            _tooltipUpdateTimer = new System.Threading.Timer(
                callback: _ => UpdateTooltip(),
                state: null,
                dueTime: TimeSpan.FromSeconds(1),
                period: TimeSpan.FromSeconds(5)
            );
        }

        private void UpdateTooltip()
        {
            try
            {
                if (_trayIcon != null)
                {
                    string tooltipText = "Claude CLI Session Manager";

                    // Show next send time if scheduled
                    if (_nextScheduledSend != DateTime.MinValue)
                    {
                        DateTime now = DateTime.Now;
                        TimeSpan timeUntil = _nextScheduledSend - now;
                        
                        if (timeUntil.TotalSeconds > 0)
                        {
                            if (timeUntil.TotalHours < 1)
                            {
                                tooltipText += $"\nNext send: {(int)timeUntil.TotalMinutes}m";
                            }
                            else if (timeUntil.TotalDays < 1)
                            {
                                tooltipText += $"\nNext send: {(int)timeUntil.TotalHours}h {timeUntil.Minutes}m";
                            }
                            else
                            {
                                tooltipText += $"\nNext send: {_nextScheduledSend:MMM d HH:mm}";
                            }
                        }
                        else
                        {
                            tooltipText += "\nNext send: Due now";
                        }
                    }
                    else
                    {
                        tooltipText += "\nNext send: Not scheduled";
                    }

                    // Show configuration status
                    if (string.IsNullOrEmpty(_claudeCliCommand))
                    {
                        tooltipText += "\n⚠ CLI not configured";
                    }
                    else
                    {
                        // Show message being sent
                        string message = string.IsNullOrEmpty(_messageToSend) ? "hi" : _messageToSend;
                        if (message.Length > 10)
                        {
                            message = message.Substring(0, 10) + "...";
                        }
                        tooltipText += $"\nMessage: '{message}'";
                        
                        // Show last status
                        if (_lastSentTime != DateTime.MinValue)
                        {
                            string statusIcon = _lastCommandSuccessful ? "✓" : "⚠";
                            tooltipText += $"\nLast: {statusIcon} {_lastSentTime:HH:mm}";
                        }
                        else
                        {
                            tooltipText += "\nStatus: Ready";
                        }
                    }

                    // Tooltip text is limited to 127 characters in some Windows versions
                    if (tooltipText.Length > 127)
                    {
                        tooltipText = tooltipText.Substring(0, 124) + "...";
                    }

                    _trayIcon!.Text = tooltipText;
                }
            }
            catch
            {
                // Silently fail tooltip updates
            }
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
            {
                return $"{(int)duration.TotalDays}d {duration.Hours}h";
            }
            else if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }
            else
            {
                return $"{duration.Minutes}m {duration.Seconds}s";
            }
        }

        private DateTime ParseClaudeSessionReset(string response)
        {
            if (string.IsNullOrEmpty(response))
                return DateTime.MinValue;

            try
            {
                // Look for patterns like "resets 1pm", "resets 13:00", "resets at 1:30pm", etc.
                var patterns = new[]
                {
                    @"resets?\s+(?:at\s+)?(\d{1,2}):?(\d{0,2})\s*(pm|am)",  // "resets 1pm", "resets at 1:30pm"
                    @"resets?\s+(?:at\s+)?(\d{1,2}):(\d{2})",  // "resets 13:00", "resets at 13:30"
                    @"resets?\s+(?:at\s+)?(\d{1,2})\s*(pm|am)",  // "resets 1pm"
                };

                foreach (var pattern in patterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(response, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        int hour = int.Parse(match.Groups[1].Value);
                        int minute = 0;
                        bool isPm = false;

                        // Parse minutes if present
                        if (match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value))
                        {
                            if (int.TryParse(match.Groups[2].Value, out int parsedMinute))
                            {
                                minute = parsedMinute;
                            }
                        }

                        // Parse AM/PM if present
                        if (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value))
                        {
                            isPm = match.Groups[3].Value.ToLower() == "pm";
                        }

                        // Convert to 24-hour format
                        if (isPm && hour != 12)
                        {
                            hour += 12;
                        }
                        else if (!isPm && hour == 12)
                        {
                            hour = 0;
                        }

                        // Create the reset time for today
                        var today = DateTime.Today;
                        var resetTime = new DateTime(today.Year, today.Month, today.Day, hour, minute, 1); // Add 1 second

                        // If the reset time has already passed today, schedule for tomorrow
                        if (resetTime <= DateTime.Now)
                        {
                            resetTime = resetTime.AddDays(1);
                        }

                        return resetTime;
                    }
                }
            }
            catch
            {
                // If parsing fails, fall back to default scheduling
            }

            return DateTime.MinValue;
        }

        private void ScheduleNextSend(string? claudeResponse = null)
        {
            DateTime scheduledTime = DateTime.MinValue;

            // Try to parse Claude's response for reset time
            if (!string.IsNullOrEmpty(claudeResponse))
            {
                scheduledTime = ParseClaudeSessionReset(claudeResponse);
            }

            // If no valid time found in response, use configurable duration from now
            if (scheduledTime == DateTime.MinValue)
            {
                scheduledTime = DateTime.Now.Add(_sessionDuration);
            }

            _nextScheduledSend = scheduledTime;
            SaveSessionData(); // Save the scheduled time

            // Update the timer to trigger at the scheduled time
            var delay = _nextScheduledSend - DateTime.Now;
            if (delay.TotalMilliseconds > 0)
            {
                _commandTimer?.Dispose();
                _commandTimer = new System.Threading.Timer(OnTimerElapsed, null, delay, Timeout.InfiniteTimeSpan);
            }
        }

        private async void SendHiCommand()
        {
            try
            {
                DateTime now = DateTime.Now;

                // Update session tracking
                if (_currentSessionStartTime == DateTime.MinValue)
                {
                    _currentSessionStartTime = now;
                }

                // Try to send to existing manual CLI windows
                bool success = await SendMessageToManualWindows(_messageToSend);

                // If no CLI windows found and CLI command is configured, launch a new one
                if (!success && !string.IsNullOrEmpty(_claudeCliCommand))
                {
                    _lastResponse = $"No Claude CLI windows found. Launching new CLI window at {DateTime.Now:HH:mm:ss}";
                    LaunchClaudeCliWindow();
                    
                    // Wait a moment for the CLI to start, then try to send the message
                    await Task.Delay(3000); // Wait 3 seconds for CLI to initialize
                    success = await SendMessageToManualWindows(_messageToSend);
                    
                    if (success)
                    {
                        _lastResponse = $"New CLI launched and message '{_messageToSend}' sent at {DateTime.Now:HH:mm:ss}";
                    }
                    else
                    {
                        _lastResponse = $"New CLI launched but failed to send message '{_messageToSend}' at {DateTime.Now:HH:mm:ss}";
                    }
                }
                else if (!success)
                {
                    // Store the response for no windows found case
                    _lastResponse = string.IsNullOrEmpty(_claudeCliCommand) ? 
                        $"No Claude CLI configured and no windows found at {DateTime.Now:HH:mm:ss}" :
                        $"No Claude CLI windows found to send message to at {DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    // Store the response for successful send
                    _lastResponse = $"Message '{_messageToSend}' sent to {FindClaudeCliWindows().Count} CLI window(s) at {DateTime.Now:HH:mm:ss}";
                }
                _lastCommandSuccessful = success;

                _lastSentTime = DateTime.Now;
                SaveLastSentTime();
                SaveSessionData();

                // Schedule the next send automatically using the configured duration
                ScheduleNextSend();

                // Show notification with appropriate status
                string statusText;
                ToolTipIcon statusIcon;
                
                if (success)
                {
                    statusText = "Success";
                    statusIcon = ToolTipIcon.Info;
                }
                else if (string.IsNullOrEmpty(_claudeCliCommand))
                {
                    statusText = "CLI Not Configured";
                    statusIcon = ToolTipIcon.Warning;
                }
                else
                {
                    statusText = "CLI Launch Attempted";
                    statusIcon = ToolTipIcon.Warning;
                }
                
                _trayIcon.ShowBalloonTip(4000, $"Message Send - {statusText}",
                    _lastResponse,
                    statusIcon);

                UpdateLastSentInMenu();
                UpdateTooltip();
            }
            catch (Exception ex)
            {
                _lastResponse = $"Exception: {ex.Message}";
                _lastCommandSuccessful = false;
                _trayIcon.ShowBalloonTip(3000, "Error",
                    $"Failed to send command: {ex.Message}",
                    ToolTipIcon.Error);
            }
        }

        private void LaunchClaudeCliWindow()
        {
            try
            {
                if (string.IsNullOrEmpty(_claudeCliCommand))
                {
                    MessageBox.Show(
                        "No Claude CLI command configured.\n\nPlease configure your Claude CLI command first.",
                        "CLI Not Configured",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // Create a new Command Prompt window with Claude CLI
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k echo Starting Claude CLI session... && echo. && {_claudeCliCommand}",
                    UseShellExecute = true,  // This will show the window
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = GetEffectiveWorkingDirectory()
                };

                Process.Start(startInfo);

                // Update session tracking
                DateTime now = DateTime.Now;
                bool isNewSession = false;

                if (_currentSessionStartTime == DateTime.MinValue || 
                    now >= _currentSessionStartTime.Add(_sessionDuration))
                {
                    _currentSessionStartTime = now;
                    isNewSession = true;
                    
                    SaveSessionData();
                }

                _lastSentTime = now;
                _lastResponse = $"Claude CLI window launched at {now:HH:mm:ss}";
                _lastCommandSuccessful = true;
                SaveLastSentTime();

                string sessionText = isNewSession ? "New session started" : "Session continued";
                _trayIcon.ShowBalloonTip(3000, 
                    "🪟 Claude CLI Window Launched", 
                    $"{sessionText}. Claude CLI opened in new window.",
                    ToolTipIcon.Info);

                UpdateLastSentInMenu();
                UpdateTooltip();
            }
            catch (Exception ex)
            {
                _trayIcon.ShowBalloonTip(3000, "Launch Error",
                    $"Failed to launch CLI window: {ex.Message}",
                    ToolTipIcon.Error);
            }
        }

        private void ToggleAutoLaunchOnStartup(ToolStripMenuItem menuItem)
        {
            _autoLaunchOnStartup = !_autoLaunchOnStartup;
            menuItem.Checked = _autoLaunchOnStartup;
            SaveCliConfiguration();

            string message = _autoLaunchOnStartup ?
                "Claude CLI will automatically launch when the service starts." :
                "Claude CLI will not automatically launch on startup.";

            _trayIcon.ShowBalloonTip(2000, "Auto-Launch", message, ToolTipIcon.Info);
        }

        private List<IntPtr> FindClaudeCliWindows()
        {
            var claudeWindows = new List<IntPtr>();
            
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd) && GetWindow(hWnd, GW_OWNER) == IntPtr.Zero)
                {
                    int length = GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        var builder = new StringBuilder(length + 1);
                        GetWindowText(hWnd, builder, builder.Capacity);
                        string windowTitle = builder.ToString();

                        // Look for various terminal windows that might be running Claude CLI
                        if (windowTitle.Contains("Command Prompt") || windowTitle.Contains("cmd.exe") || 
                            windowTitle.Contains("Windows PowerShell") || windowTitle.Contains("PowerShell") ||
                            windowTitle.Contains("Windows Terminal") || windowTitle.Contains("Terminal") ||
                            windowTitle.Contains("ConEmu") || windowTitle.Contains("Cmder") ||
                            windowTitle.Contains("GitBash") || windowTitle.Contains("Git Bash") ||
                            windowTitle.Contains("claude", StringComparison.OrdinalIgnoreCase))
                        {
                            // Additional check: see if this window belongs to a terminal process
                            GetWindowThreadProcessId(hWnd, out uint processId);
                            try
                            {
                                var process = Process.GetProcessById((int)processId);
                                string processName = process.ProcessName.ToLower();
                                if (processName == "cmd" || processName == "powershell" || processName == "pwsh" ||
                                    processName == "windowsterminal" || processName == "wt" ||
                                    processName == "conemu" || processName == "conemu64" ||
                                    processName == "cmder" || processName == "mintty" ||
                                    processName == "bash" || processName == "git-bash")
                                {
                                    claudeWindows.Add(hWnd);
                                }
                            }
                            catch
                            {
                                // If we can't get process info, skip this window
                            }
                        }
                    }
                }
                return true; // Continue enumeration
            }, IntPtr.Zero);

            return claudeWindows;
        }

        private async Task<bool> SendMessageToManualWindows(string message)
        {
            try
            {
                var claudeWindows = FindClaudeCliWindows();
                if (!claudeWindows.Any())
                {
                    return false; // No manual windows found
                }

                int successCount = 0;
                foreach (var windowHandle in claudeWindows)
                {
                    try
                    {
                        // Bring window to foreground
                        ShowWindow(windowHandle, SW_RESTORE);
                        SetForegroundWindow(windowHandle);
                        
                        // Small delay to ensure window is ready
                        await Task.Delay(100);
                        
                        // Send the message by simulating typing
                        SendKeys.SendWait(message);
                        await Task.Delay(50);
                        
                        // Press Enter
                        keybd_event(VK_RETURN, 0, 0, IntPtr.Zero);
                        await Task.Delay(50);
                        keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
                        
                        successCount++;
                        await Task.Delay(200); // Brief pause between windows
                    }
                    catch
                    {
                        // Continue with other windows if one fails
                        continue;
                    }
                }

                return successCount > 0;
            }
            catch (Exception ex)
            {
                _lastResponse = $"Error sending to manual windows: {ex.Message}";
                return false;
            }
        }

        private void ShowLastResponse()
        {
            if (string.IsNullOrEmpty(_lastResponse))
            {
                MessageBox.Show(
                    "No response has been received yet.\n\nTry sending a message first.",
                    "No Response Available",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            string statusIcon = _lastCommandSuccessful ? "✓" : "⚠";
            string statusText = _lastCommandSuccessful ? "Successful" : "Warning/Error";
            
            // Create a form for better response display
            var responseForm = new Form
            {
                Text = "Last Claude Response",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterScreen,
                MinimumSize = new Size(400, 300),
                ShowIcon = false,
                ShowInTaskbar = false
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var headerLabel = new Label
            {
                Text = $"{statusIcon} Command Status: {statusText}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            var timeLabel = new Label
            {
                Text = $"Sent at: {(_lastSentTime == DateTime.MinValue ? "Unknown" : _lastSentTime.ToString("yyyy-MM-dd HH:mm:ss"))}",
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(0, 25),
                ForeColor = Color.Gray
            };

            var responseLabel = new Label
            {
                Text = "Response:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 50)
            };

            var responseTextBox = new TextBox
            {
                Text = _lastResponse,
                Location = new Point(0, 75),
                Size = new Size(560, 250),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            var closeButton = new Button
            {
                Text = "Close",
                Size = new Size(75, 30),
                Location = new Point(485, 335),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };

            panel.Controls.Add(headerLabel);
            panel.Controls.Add(timeLabel);
            panel.Controls.Add(responseLabel);
            panel.Controls.Add(responseTextBox);
            panel.Controls.Add(closeButton);
            
            responseForm.Controls.Add(panel);
            responseForm.AcceptButton = closeButton;
            
            responseForm.ShowDialog();
        }

        private void OnTrayIconDoubleClick(object? sender, EventArgs e)
        {
            string statusInfo = "";
            if (_lastSentTime != DateTime.MinValue)
            {
                string statusIcon = _lastCommandSuccessful ? "✓" : "⚠";
                string statusText = _lastCommandSuccessful ? "Success" : "Warning/Error";
                statusInfo = $"Last command: {statusIcon} {statusText} at {_lastSentTime:yyyy-MM-dd HH:mm:ss}\n";
            }

            string sessionInfo = "";
            if (_nextScheduledSend != DateTime.MinValue)
            {
                DateTime now = DateTime.Now;
                TimeSpan timeUntilNext = _nextScheduledSend - now;
                
                if (timeUntilNext.TotalSeconds > 0)
                {
                    sessionInfo = $"Next message: {FormatDuration(timeUntilNext)} ({_nextScheduledSend:yyyy-MM-dd HH:mm})\n" +
                                 $"Session started: {(_currentSessionStartTime != DateTime.MinValue ? _currentSessionStartTime.ToString("yyyy-MM-dd HH:mm:ss") : "Not started")}\n";
                }
                else
                {
                    sessionInfo = "Next message: Due now\n";
                }
            }
            else
            {
                sessionInfo = "Next message: Not scheduled\n";
            }

            MessageBox.Show(
                $"Claude CLI Session Manager\n\n" +
                $"Automatically sends messages to Claude CLI windows at configured intervals.\n" +
                $"If no CLI windows are found, a new one will be launched automatically.\n" +
                $"Helps maintain active Claude sessions for maximum productivity.\n\n" +
                sessionInfo +
                statusInfo +
                $"\nRight-click the tray icon for options.",
                "Claude CLI Session Manager",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void SetStartupRegistry()
        {
            try
            {
                if (!IsInStartup())
                {
                    using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null)
                        {
                            key.SetValue(APP_NAME, $"\"{Application.ExecutablePath}\"");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _trayIcon.ShowBalloonTip(3000, "Startup Error",
                    $"Could not set Windows startup: {ex.Message}",
                    ToolTipIcon.Warning);
            }
        }

        private bool IsInStartup()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key?.GetValue(APP_NAME) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ToggleStartup(ToolStripMenuItem menuItem)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (IsInStartup())
                        {
                            key.DeleteValue(APP_NAME, false);
                            menuItem.Checked = false;
                            _trayIcon.ShowBalloonTip(2000, "Startup Disabled", "Application will not start with Windows.", ToolTipIcon.Info);
                        }
                        else
                        {
                            key.SetValue(APP_NAME, $"\"{Application.ExecutablePath}\"");
                            menuItem.Checked = true;
                            _trayIcon.ShowBalloonTip(2000, "Startup Enabled", "Application will start with Windows.", ToolTipIcon.Info);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLastSentTime()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    APP_NAME
                );

                string timestampFile = Path.Combine(appDataPath, "lastsent.txt");

                if (File.Exists(timestampFile))
                {
                    string content = File.ReadAllText(timestampFile);
                    if (DateTime.TryParse(content, out DateTime lastTime))
                    {
                        _lastSentTime = lastTime;
                    }
                }
            }
            catch
            {
                _lastSentTime = DateTime.MinValue;
            }
        }

        private void SaveLastSentTime()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    APP_NAME
                );

                Directory.CreateDirectory(appDataPath);
                string timestampFile = Path.Combine(appDataPath, "lastsent.txt");
                File.WriteAllText(timestampFile, _lastSentTime.ToString("O"));
            }
            catch
            {
                // Silently fail
            }
        }

        private void LoadSessionData()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    APP_NAME
                );

                string sessionFile = Path.Combine(appDataPath, "session.txt");

                if (File.Exists(sessionFile))
                {
                    string[] lines = File.ReadAllLines(sessionFile);
                    if (lines.Length >= 1)
                    {
                        if (DateTime.TryParse(lines[0], out DateTime sessionStart))
                        {
                            _currentSessionStartTime = sessionStart;
                        }
                        
                        // Load response data if available
                        if (lines.Length >= 3)
                        {
                            _lastResponse = lines[1] ?? "";
                            if (bool.TryParse(lines[2], out bool success))
                            {
                                _lastCommandSuccessful = success;
                            }
                        }

                        // Load next scheduled send time if available
                        if (lines.Length >= 4)
                        {
                            if (DateTime.TryParse(lines[3], out DateTime nextSend))
                            {
                                _nextScheduledSend = nextSend;
                            }
                        }

                        // Load session duration if available (new field)
                        if (lines.Length >= 5)
                        {
                            if (double.TryParse(lines[4], out double durationHours))
                            {
                                _sessionDuration = TimeSpan.FromHours(durationHours);
                            }
                        }
                    }
                }
            }
            catch
            {
                _currentSessionStartTime = DateTime.MinValue;
                _nextScheduledSend = DateTime.MinValue;
                _sessionDuration = TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(1)); // Default
            }
        }

        private void SaveSessionData()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    APP_NAME
                );

                Directory.CreateDirectory(appDataPath);
                string sessionFile = Path.Combine(appDataPath, "session.txt");

                string[] lines = new string[]
                {
                    _currentSessionStartTime.ToString("O"),
                    _lastResponse ?? "",
                    _lastCommandSuccessful.ToString(),
                    _nextScheduledSend.ToString("O"),
                    _sessionDuration.TotalHours.ToString() // Save duration in hours
                };

                File.WriteAllLines(sessionFile, lines);
            }
            catch
            {
                // Silently fail
            }
        }

        private void UpdateLastSentInMenu()
        {
            if (_trayIcon.ContextMenuStrip != null)
            {
                var lastSentItem = _trayIcon.ContextMenuStrip.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == "lastSent");

                if (lastSentItem != null)
                {
                    UpdateLastSentMenuItem(lastSentItem);
                }

                var nextSendItem = _trayIcon.ContextMenuStrip.Items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(i => i.Tag?.ToString() == "nextSend");

                if (nextSendItem != null)
                {
                    UpdateNextSendMenuItem(nextSendItem);
                }
            }
        }

        private void UpdateLastSentMenuItem(ToolStripMenuItem item)
        {
            if (_lastSentTime == DateTime.MinValue)
            {
                item.Text = "Last Sent: Never";
            }
            else
            {
                item.Text = $"Last Sent: {_lastSentTime:yyyy-MM-dd HH:mm:ss}";
            }
        }

        private void UpdateNextSendMenuItem(ToolStripMenuItem item)
        {
            if (_nextScheduledSend == DateTime.MinValue)
            {
                item.Text = "Next Send: Not scheduled";
            }
            else
            {
                TimeSpan timeUntil = _nextScheduledSend - DateTime.Now;
                if (timeUntil.TotalSeconds <= 0)
                {
                    item.Text = "Next Send: Due now";
                }
                else if (timeUntil.TotalHours < 1)
                {
                    item.Text = $"Next Send: in {(int)timeUntil.TotalMinutes}m ({_nextScheduledSend:HH:mm})";
                }
                else if (timeUntil.TotalDays < 1)
                {
                    item.Text = $"Next Send: in {(int)timeUntil.TotalHours}h {timeUntil.Minutes}m ({_nextScheduledSend:HH:mm})";
                }
                else
                {
                    item.Text = $"Next Send: {_nextScheduledSend:yyyy-MM-dd HH:mm}";
                }
            }
        }

        private void ConfigureClaudeCli()
        {
            var form = new Form
            {
                Text = "Configure Claude CLI Command",
                Size = new Size(500, 250),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowIcon = false
            };

            var label = new Label
            {
                Text = "Enter your Claude CLI command:",
                Location = new Point(15, 20),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(15, 45),
                Size = new Size(450, 23),
                Text = _claudeCliCommand,
                PlaceholderText = "e.g., claude, claude-cli, or path to your Claude CLI executable"
            };

            var exampleLabel = new Label
            {
                Text = "Examples:\n• claude\n• claude-cli\n• C:\\path\\to\\claude.exe\n• python claude_wrapper.py",
                Location = new Point(15, 75),
                Size = new Size(450, 80),
                Font = new Font("Consolas", 8)
            };

            var testButton = new Button
            {
                Text = "Test CLI",
                Location = new Point(15, 165),
                Size = new Size(75, 25)
            };

            testButton.Click += async (s, e) => 
            {
                testButton.Enabled = false;
                testButton.Text = "Testing...";
                
                string testCommand = textBox.Text.Trim();
                if (string.IsNullOrEmpty(testCommand))
                {
                    MessageBox.Show("Please enter a CLI command first.", "Test CLI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    testButton.Enabled = true;
                    testButton.Text = "Test CLI";
                    return;
                }

                try
                {
                    var testStartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {testCommand} --version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var testProcess = Process.Start(testStartInfo);
                    if (testProcess != null)
                    {
                        bool finished = await Task.Run(() => testProcess.WaitForExit(10000));
                        string output = await testProcess.StandardOutput.ReadToEndAsync();
                        string error = await testProcess.StandardError.ReadToEndAsync();

                        if (finished && testProcess.ExitCode == 0)
                        {
                            MessageBox.Show($"CLI test successful!\n\nOutput: {output}", "Test CLI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show($"CLI test failed or timed out.\n\nError: {error}", "Test CLI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"CLI test error: {ex.Message}", "Test CLI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                testButton.Enabled = true;
                testButton.Text = "Test CLI";
            };

            var okButton = new Button
            {
                Text = "Save",
                Location = new Point(320, 165),
                Size = new Size(75, 25),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(400, 165),
                Size = new Size(75, 25),
                DialogResult = DialogResult.Cancel
            };

            form.Controls.AddRange(new Control[] { label, textBox, exampleLabel, testButton, okButton, cancelButton });
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            if (form.ShowDialog() == DialogResult.OK)
            {
                _claudeCliCommand = textBox.Text.Trim();
                SaveCliConfiguration();
                
                string message = string.IsNullOrEmpty(_claudeCliCommand) ? 
                    "Claude CLI command cleared." : 
                    $"Claude CLI command saved: '{_claudeCliCommand}'";
                    
                _trayIcon.ShowBalloonTip(2000, "CLI Configuration", message, ToolTipIcon.Info);
            }
        }

        private void ConfigureMessage()
        {
            var form = new Form
            {
                Text = "Configure Message",
                Size = new Size(450, 280),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowIcon = false
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            var label = new Label
            {
                Text = "Enter the message to send to Claude CLI windows:",
                Location = new Point(20, 10),
                Size = new Size(400, 20),
                AutoSize = false
            };

            var textBox = new TextBox
            {
                Location = new Point(20, 35),
                Size = new Size(390, 23),
                Text = _messageToSend,
                PlaceholderText = "e.g., hi, hello, continue"
            };

            var exampleLabel = new Label
            {
                Text = "Examples:\n• hi (default - simple greeting)\n• hello (alternative greeting)\n• continue (to continue previous conversation)\n• status (to check Claude's status)",
                Location = new Point(20, 70),
                Size = new Size(390, 80),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            var noteLabel = new Label
            {
                Text = "Note: This message will be automatically sent to all Claude CLI windows\nat the configured intervals to keep sessions active.",
                Location = new Point(20, 160),
                Size = new Size(390, 40),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.DarkBlue
            };

            var okButton = new Button
            {
                Text = "Save",
                Location = new Point(240, 210),
                Size = new Size(75, 25),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(320, 210),
                Size = new Size(75, 25),
                DialogResult = DialogResult.Cancel
            };

            panel.Controls.AddRange(new Control[] { label, textBox, exampleLabel, noteLabel, okButton, cancelButton });
            form.Controls.Add(panel);
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            if (form.ShowDialog() == DialogResult.OK)
            {
                string newMessage = textBox.Text.Trim();
                
                // Set default if empty
                if (string.IsNullOrEmpty(newMessage))
                {
                    newMessage = "hi";
                }

                _messageToSend = newMessage;
                SaveCliConfiguration();
                
                string message = $"Message configured: '{_messageToSend}'";
                _trayIcon.ShowBalloonTip(2000, "Message Configuration", message, ToolTipIcon.Info);
            }
        }

        private void ConfigureWorkingDirectory()
        {
            var form = new Form
            {
                Text = "Configure Working Directory",
                Size = new Size(580, 380),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowIcon = false
            };

            var label = new Label
            {
                Text = "Enter the working directory for Claude CLI windows:",
                Location = new Point(15, 20),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(15, 45),
                Size = new Size(450, 23),
                Text = _workingDirectory,
                PlaceholderText = "Leave empty to use default (User Profile directory)"
            };

            var browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(470, 45),
                Size = new Size(60, 23)
            };

            browseButton.Click += (s, e) =>
            {
                using var folderDialog = new FolderBrowserDialog
                {
                    Description = "Select working directory for Claude CLI windows",
                    ShowNewFolderButton = true,
                    SelectedPath = string.IsNullOrEmpty(textBox.Text) ? 
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : 
                        textBox.Text
                };

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = folderDialog.SelectedPath;
                }
            };

            var infoLabel = new Label
            {
                Text = "Working Directory Information:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(15, 80),
                AutoSize = true
            };

            var currentDirLabel = new Label
            {
                Text = $"Current default: {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}",
                Location = new Point(15, 105),
                Size = new Size(500, 20),
                Font = new Font("Consolas", 8)
            };

            var exampleLabel = new Label
            {
                Text = "Examples:\n• C:\\Projects\n• D:\\Code\\MyProject\n• C:\\Users\\YourName\\Documents\\Claude",
                Location = new Point(15, 130),
                Size = new Size(500, 60),
                Font = new Font("Consolas", 8)
            };

            var noteLabel = new Label
            {
                Text = "Note: The working directory affects where Claude CLI will look for files\nand where relative paths will be resolved.",
                Location = new Point(15, 200),
                Size = new Size(500, 40),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.DarkBlue
            };

            var okButton = new Button
            {
                Text = "Save",
                Location = new Point(380, 290),
                Size = new Size(75, 25),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(460, 290),
                Size = new Size(75, 25),
                DialogResult = DialogResult.Cancel
            };

            form.Controls.AddRange(new Control[] { 
                label, textBox, browseButton, infoLabel, currentDirLabel, 
                exampleLabel, noteLabel, okButton, cancelButton 
            });
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            if (form.ShowDialog() == DialogResult.OK)
            {
                string newWorkingDir = textBox.Text.Trim();
                
                // Validate the directory if it's not empty
                if (!string.IsNullOrEmpty(newWorkingDir))
                {
                    try
                    {
                        if (!Directory.Exists(newWorkingDir))
                        {
                            var result = MessageBox.Show(
                                $"Directory does not exist: {newWorkingDir}\n\nWould you like to create it?",
                                "Directory Not Found",
                                MessageBoxButtons.YesNoCancel,
                                MessageBoxIcon.Question
                            );

                            if (result == DialogResult.Yes)
                            {
                                Directory.CreateDirectory(newWorkingDir);
                            }
                            else if (result == DialogResult.Cancel)
                            {
                                return; // Don't save the configuration
                            }
                            // If No, proceed anyway (user might create it later)
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error accessing directory: {ex.Message}",
                            "Directory Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        return; // Don't save the configuration
                    }
                }

                _workingDirectory = newWorkingDir;
                SaveCliConfiguration();
                
                string message = string.IsNullOrEmpty(_workingDirectory) ? 
                    "Working directory cleared. Will use default User Profile directory." : 
                    $"Working directory set to: {_workingDirectory}";
                    
                _trayIcon.ShowBalloonTip(3000, "Working Directory", message, ToolTipIcon.Info);
            }
        }

        private string GetEffectiveWorkingDirectory()
        {
            // Return custom working directory if set, otherwise use default
            if (string.IsNullOrEmpty(_workingDirectory))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            
            // Verify the directory still exists, fall back to default if not
            try
            {
                if (Directory.Exists(_workingDirectory))
                {
                    return _workingDirectory;
                }
            }
            catch
            {
                // If there's any error accessing the directory, use default
            }
            
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private void LoadCliConfiguration()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    APP_NAME
                );
                
                string configFile = Path.Combine(appDataPath, "cli_config.txt");
                
                if (File.Exists(configFile))
                {
                    string[] lines = File.ReadAllLines(configFile);
                    if (lines.Length > 0)
                    {
                        _claudeCliCommand = lines[0].Trim();
                    }
                    if (lines.Length > 1)
                    {
                        _workingDirectory = lines[1].Trim();
                    }
                    if (lines.Length > 2)
                    {
                        if (bool.TryParse(lines[2].Trim(), out bool autoLaunch))
                        {
                            _autoLaunchOnStartup = autoLaunch;
                        }
                    }
                    if (lines.Length > 3)
                    {
                        string message = lines[3].Trim();
                        if (!string.IsNullOrEmpty(message))
                        {
                            _messageToSend = message;
                        }
                    }
                }
            }
            catch
            {
                _claudeCliCommand = "claude";  // Default to "claude" command
                _workingDirectory = "";
                _autoLaunchOnStartup = false;
                _messageToSend = "hi";  // Default message
            }
        }

        private void SaveCliConfiguration()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    APP_NAME
                );

                Directory.CreateDirectory(appDataPath);
                string configFile = Path.Combine(appDataPath, "cli_config.txt");

                if (string.IsNullOrEmpty(_claudeCliCommand))
                {
                    if (File.Exists(configFile))
                        File.Delete(configFile);
                }
                else
                {
                    string[] configLines = new string[]
                    {
                        _claudeCliCommand,
                        _workingDirectory ?? "",
                        _autoLaunchOnStartup.ToString(),
                        _messageToSend ?? "hi"
                    };
                    File.WriteAllLines(configFile, configLines);
                }
            }
            catch
            {
                // Silently fail
            }
        }

        private void ConfigureSchedule()
        {
            var form = new Form
            {
                Text = "Configure Schedule",
                Size = new Size(480, 380),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowIcon = false
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            // Duration configuration
            var durationLabel = new Label
            {
                Text = "Session Duration (hours):",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var durationNumeric = new NumericUpDown
            {
                Location = new Point(20, 45),
                Size = new Size(100, 23),
                Minimum = 0.5m,
                Maximum = 24,
                DecimalPlaces = 1,
                Increment = 0.5m,
                Value = (decimal)_sessionDuration.TotalHours
            };

            var durationHelpLabel = new Label
            {
                Text = "Time between automatic messages to Claude CLI (default: 5 hours)",
                Location = new Point(20, 75),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            // Next send time configuration
            var nextSendLabel = new Label
            {
                Text = "Next Send Time:",
                Location = new Point(20, 110),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var nextSendPicker = new DateTimePicker
            {
                Location = new Point(20, 135),
                Size = new Size(200, 23),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm",
                ShowUpDown = false
            };

            // Set initial value
            if (_nextScheduledSend != DateTime.MinValue && _nextScheduledSend > DateTime.Now)
            {
                nextSendPicker.Value = _nextScheduledSend;
            }
            else
            {
                nextSendPicker.Value = DateTime.Now.AddMinutes(30);
            }

            var useDefaultButton = new Button
            {
                Text = "Use Duration",
                Location = new Point(230, 135),
                Size = new Size(100, 23)
            };

            useDefaultButton.Click += (s, e) =>
            {
                nextSendPicker.Value = DateTime.Now.Add(TimeSpan.FromHours((double)durationNumeric.Value));
            };

            var nextSendHelpLabel = new Label
            {
                Text = "Override the next scheduled send time. When this time is reached,\nthe next send will be automatically scheduled using the duration above.",
                Location = new Point(20, 165),
                Size = new Size(400, 40),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            // Current status
            var statusLabel = new Label
            {
                Text = "Current Status:",
                Location = new Point(20, 215),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            string currentStatusText = "";
            if (_nextScheduledSend != DateTime.MinValue)
            {
                if (_nextScheduledSend > DateTime.Now)
                {
                    TimeSpan timeUntil = _nextScheduledSend - DateTime.Now;
                    currentStatusText = $"Next send: {_nextScheduledSend:yyyy-MM-dd HH:mm} (in {FormatDuration(timeUntil)})";
                }
                else
                {
                    currentStatusText = "Next send: Due now";
                }
            }
            else
            {
                currentStatusText = "Next send: Not scheduled";
            }

            var statusValueLabel = new Label
            {
                Text = currentStatusText,
                Location = new Point(20, 235),
                Size = new Size(400, 20),
                Font = new Font("Consolas", 8)
            };

            var sessionDurationStatusLabel = new Label
            {
                Text = $"Current duration: {_sessionDuration.TotalHours:F1} hours",
                Location = new Point(20, 255),
                Size = new Size(400, 20),
                Font = new Font("Consolas", 8)
            };

            var okButton = new Button
            {
                Text = "Save",
                Location = new Point(260, 285),
                Size = new Size(75, 25),
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(340, 285),
                Size = new Size(75, 25),
                DialogResult = DialogResult.Cancel
            };

            panel.Controls.AddRange(new Control[] {
                durationLabel, durationNumeric, durationHelpLabel,
                nextSendLabel, nextSendPicker, useDefaultButton, nextSendHelpLabel,
                statusLabel, statusValueLabel, sessionDurationStatusLabel,
                okButton, cancelButton
            });

            form.Controls.Add(panel);
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            if (form.ShowDialog() == DialogResult.OK)
            {
                // Update session duration
                _sessionDuration = TimeSpan.FromHours((double)durationNumeric.Value);
                
                // Update next scheduled send time
                _nextScheduledSend = nextSendPicker.Value;
                
                // Save the configuration
                SaveSessionData();
                
                // Restart timer with new schedule
                RestartTimer();
                
                // Update menu items
                UpdateLastSentInMenu();
                
                string message = $"Schedule updated!\nDuration: {_sessionDuration.TotalHours:F1} hours\nNext send: {_nextScheduledSend:yyyy-MM-dd HH:mm}";
                _trayIcon.ShowBalloonTip(4000, "Schedule Configuration", message, ToolTipIcon.Info);
            }
        }

        private void OnExit(object? sender, EventArgs e)
        {
            _commandTimer?.Dispose();
            _tooltipUpdateTimer?.Dispose();
            _trayIcon.Visible = false;
            
            // Dispose of custom icon if it's not a system icon
            if (_trayIcon.Icon != null && !ReferenceEquals(_trayIcon.Icon, SystemIcons.Application))
            {
                _trayIcon.Icon.Dispose();
            }
            
            _trayIcon.Dispose();
            Application.Exit();
        }
    }
}
