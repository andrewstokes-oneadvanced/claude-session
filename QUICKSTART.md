# Quick Start Guide

## Installation

1. **Install .NET 8.0 SDK** (if not already installed)
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Install and restart your terminal

2. **Build the Application**
   - Double-click [setup.bat](setup.bat)
   - Follow the prompts

   OR manually:
   - Open Command Prompt in the project folder
   - Run: `dotnet build -c Release`

3. **Run the Application**
   - The setup script will offer to run it for you

   OR manually:
   - Navigate to: `bin\Release\net8.0-windows\`
   - Run: `ClaudeCodeHelper.exe`

## First Time Usage

1. The application starts in the system tray (look in the bottom-right corner of your screen)
2. It automatically adds itself to Windows startup
3. The first "hi" command is sent immediately, then every 5 hours

## Tooltip Information

Hover your mouse over the tray icon to see:

```
Claude Code Helper
Session: 2h 15m
Tokens: 185,432 / 200,000
```

This shows:
- **Session**: Time since the first "hi" was sent in this session
- **Tokens**: Remaining tokens out of total tokens for your Claude session

## Tray Icon Menu (Right-Click)

- **Send 'hi' Now** - Manually send a command right now
- **Last Sent** - Shows timestamp of last command sent
- **Reset Session** - Clear session timer and token counter
- **Run at Windows Startup** - Toggle automatic startup (checked = enabled)
- **Exit** - Close the application

## Configuration Files

The application stores data in:
```
%AppData%\ClaudeCodeHelper\
```

Files:
- `lastsent.txt` - Timestamp of last command sent
- `session.txt` - Session start time and token information

## Troubleshooting

### The tray icon doesn't show token information

The application tries to parse token usage from Claude Code's output. If it can't find token information:
- Make sure Claude Code CLI is installed: `claude --version`
- Token info may not be available in the command output
- You can still use the app - it will show "Tokens: Checking..."

### Commands aren't being sent

1. Verify Claude Code is installed: Open Command Prompt and type `claude`
2. Check if the command works manually: `echo hi | claude`
3. Right-click the tray icon and select "Send 'hi' Now" to test

### Application isn't starting with Windows

1. Run the application as Administrator once
2. Right-click the tray icon
3. Ensure "Run at Windows Startup" is checked

## Customization

Want to change settings? Edit [TrayApplicationContext.cs](TrayApplicationContext.cs):

- **Change interval**: Line 17 - `_interval = TimeSpan.FromHours(5);`
- **Change command**: Line 181 - `Arguments = "/c echo hi | claude"`
- **Change tooltip update rate**: Line 102 - `period: TimeSpan.FromSeconds(5)`

After editing, rebuild with [build.bat](build.bat).

## Support

For issues or questions:
- Check the main [README.md](README.md) for detailed information
- Review the source code - it's well commented!
- Verify Claude Code CLI is working properly

## Uninstallation

1. Right-click tray icon and select "Exit"
2. Press Win+R, type: `regedit`
3. Navigate to: `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
4. Delete the "ClaudeCodeHelper" entry
5. Delete the application folder
6. Delete: `%AppData%\ClaudeCodeHelper\`
