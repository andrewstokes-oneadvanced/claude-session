# User Guide

## What is Claude Code Helper?

Claude Code Helper is a lightweight Windows application that keeps your Claude Code session active by automatically sending a "hi" command every 5 hours. It runs silently in your system tray and provides real-time information about your session.

## Installation

### Step 1: Check Prerequisites

Open Command Prompt and verify:

```batch
dotnet --version
```

If you see a version number (8.0 or higher), you're good to go!

If not, install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0

### Step 2: Build the Application

Double-click `setup.bat` in the ClaudeCodeHelper folder.

The script will:
- Build the application
- Ask if you want to run it now

### Step 3: Verify Installation

Look in your system tray (bottom-right corner of Windows taskbar). You should see a new icon.

## Using the Application

### System Tray Icon

The application runs entirely from the system tray. You'll never see a main window.

**Finding the icon:**
1. Look in the bottom-right corner of your Windows taskbar
2. You may need to click the "^" arrow to show hidden icons
3. The icon appears as a default application icon (generic window)

### Tooltip Information (Hover)

When you hover your mouse over the tray icon, you'll see:

```
Claude Code Helper
Session: 2h 35m 12s
Tokens: 175,423 / 200,000
```

**What this means:**
- **Session**: How long since the first "hi" was sent
- **Tokens**: How many tokens remain in your Claude session

The tooltip updates automatically every 5 seconds.

### Right-Click Menu

Right-click the tray icon to see these options:

```
┌─────────────────────────────┐
│ Send 'hi' Now               │
│ Last Sent: 2025-11-08 14:30 │
├─────────────────────────────┤
│ Reset Session               │
├─────────────────────────────┤
│ ✓ Run at Windows Startup    │
├─────────────────────────────┤
│ Exit                        │
└─────────────────────────────┘
```

#### Menu Options Explained

**Send 'hi' Now**
- Immediately sends a "hi" command to Claude Code
- Useful for testing or resetting the 5-hour timer
- Shows a notification balloon when complete

**Last Sent**
- Displays when the last command was sent
- This is a status item (not clickable)
- Updates automatically after each send

**Reset Session**
- Clears the session timer back to zero
- Resets token counter
- Useful when starting a new Claude Code session
- Asks for confirmation before resetting

**Run at Windows Startup**
- Checkmark (✓) means it will start automatically
- Click to toggle on/off
- When enabled, app starts when you log into Windows
- Modifies Windows registry (current user only)

**Exit**
- Closes the application completely
- Removes the tray icon
- Stops sending commands
- Session data is saved for next time

### Double-Click Action

Double-clicking the tray icon shows a dialog box with:

```
┌──────────────────────────────────────┐
│ Claude Code Helper                   │
│                                      │
│ Sends 'hi' to Claude Code every 5    │
│ hours.                               │
│                                      │
│ Last sent: 2025-11-08 14:30:45       │
│ Next scheduled: 19:30:45             │
│                                      │
│              [ OK ]                  │
└──────────────────────────────────────┘
```

## Notifications

The application shows Windows notification balloons for:

### Startup
```
Claude Code Helper
Application started. Will send 'hi' to Claude Code every 5 hours.
```

### Command Sent
```
Command Sent
'hi' command sent to Claude Code at 14:30:45
```

### Errors
```
Error
Failed to send command: [error message]
```

### Session Reset
```
Session Reset
Session timer and token counter have been reset.
```

### Startup Toggle
```
Startup Enabled
Application will start with Windows.
```

## Understanding Session Information

### Session Timer

**When it starts:**
- The first time a "hi" command is sent
- Persists across application restarts

**When to reset:**
- When you start a new Claude Code conversation
- When you want to track a new session
- After token limit is reached

**Display format:**
- Under 1 hour: "35m 12s"
- Under 1 day: "2h 35m"
- Over 1 day: "3d 5h"

### Token Counter

**What it shows:**
- Remaining tokens / Total tokens
- Example: "175,423 / 200,000"

**How it works:**
- Application tries to parse token info from Claude Code's output
- Updates after each command is sent
- May show "Checking..." if unable to parse

**Note:** Token tracking depends on Claude Code CLI providing this information in its output. If not available, this feature will show "Tokens: Checking..."

## Common Tasks

### Starting with Windows

**To Enable:**
1. Right-click tray icon
2. Click "Run at Windows Startup"
3. Verify checkmark appears

**To Disable:**
1. Right-click tray icon
2. Click "Run at Windows Startup" again
3. Verify checkmark disappears

The application is automatically set to start with Windows on first run.

### Manually Sending a Command

**When you might want to:**
- Testing if Claude Code is working
- Resetting the 5-hour timer
- Keeping session alive before a long period

**How to:**
1. Right-click tray icon
2. Click "Send 'hi' Now"
3. Wait for notification balloon

### Resetting Session Data

**When to reset:**
- Starting a new Claude Code session
- Token counter is inaccurate
- Want to track a new session period

**How to:**
1. Right-click tray icon
2. Click "Reset Session"
3. Click "Yes" in confirmation dialog
4. Session timer and tokens are cleared

### Exiting the Application

**Method 1: Via Menu**
1. Right-click tray icon
2. Click "Exit"

**Method 2: Task Manager**
1. Open Task Manager (Ctrl+Shift+Esc)
2. Find "ClaudeCodeHelper"
3. Click "End Task"

## Troubleshooting

### Icon Not Visible in System Tray

**Check hidden icons:**
1. Click the "^" arrow in the system tray
2. Look for the ClaudeCodeHelper icon
3. Drag it to the main tray area to pin it

**Verify it's running:**
1. Open Task Manager (Ctrl+Shift+Esc)
2. Look for "ClaudeCodeHelper.exe" in the Processes tab

### Commands Not Being Sent

**Test Claude Code CLI:**
1. Open Command Prompt
2. Type: `claude --version`
3. If error, reinstall Claude Code

**Test manual command:**
1. Open Command Prompt
2. Type: `echo hi | claude`
3. Verify it works

**Check application:**
1. Right-click tray icon
2. Click "Send 'hi' Now"
3. Watch for notification or error

### Token Information Not Showing

This is normal if Claude Code CLI doesn't output token information in a parseable format. The application will still work fine for sending commands.

**What you'll see:**
```
Claude Code Helper
Session: 1h 23m
Tokens: Checking...
```

### Application Not Starting with Windows

**Fix the registry entry:**
1. Run the application
2. Right-click tray icon
3. Uncheck "Run at Windows Startup"
4. Check it again
5. Restart your computer to test

**Manual verification:**
1. Press Win+R
2. Type: `shell:startup`
3. Or check registry: `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`

### Notification Balloons Not Appearing

Windows may suppress notifications. To enable:
1. Open Settings → System → Notifications
2. Ensure notifications are enabled
3. Scroll down to find ClaudeCodeHelper
4. Enable notifications for it

## Data and Privacy

### What Data is Stored?

**Location:** `%AppData%\ClaudeCodeHelper\`

**Files:**
1. `lastsent.txt` - Timestamp of last command
2. `session.txt` - Session start time and token counts

### Privacy Notes

- No data is sent to the internet
- Only communicates with local Claude Code CLI
- All data stays on your computer
- No telemetry or analytics

### Uninstalling

**To completely remove:**
1. Exit the application (right-click → Exit)
2. Delete the application folder
3. Delete: `%AppData%\ClaudeCodeHelper\`
4. Remove from registry:
   - Press Win+R, type `regedit`
   - Go to: `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
   - Delete "ClaudeCodeHelper" entry

## Tips and Tricks

### Minimize Notifications

If notification balloons are distracting:
1. Windows Settings → System → Notifications
2. Find ClaudeCodeHelper
3. Disable notifications
4. You can still see status via tooltip

### Check if It's Working

**Quick check:**
1. Hover over tray icon
2. Note the "Last Sent" time in tooltip
3. Right-click to see detailed timestamp

**Force a test:**
1. Right-click → "Send 'hi' Now"
2. Watch for notification balloon
3. Check timestamp updated

### Multiple Sessions

If you run multiple Claude Code instances:
- The app sends to whichever responds to the `claude` command
- Each send resets the 5-hour timer
- Consider adjusting the interval if needed

## Support and Help

### Before Asking for Help

1. Verify .NET 8.0 is installed: `dotnet --version`
2. Verify Claude Code works: `echo hi | claude`
3. Check Task Manager - is ClaudeCodeHelper.exe running?
4. Try "Send 'hi' Now" manually

### Common Questions

**Q: Can I change the 5-hour interval?**
A: Yes, but requires editing the source code. See [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) for details.

**Q: Does this work with Claude API directly?**
A: No, it only works with Claude Code CLI installed on your system.

**Q: Will this keep my Claude session alive forever?**
A: It sends commands every 5 hours. Check Claude's session policies.

**Q: Can I run multiple instances?**
A: Not recommended. They would conflict with each other.

**Q: Is this officially supported by Anthropic?**
A: No, this is a community-created tool.

## Advanced Usage

See [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) for:
- Source code structure
- Customization options
- Build process details
- Extension ideas
