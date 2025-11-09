# Project Structure

## Overview

ClaudeCodeHelper is a Windows system tray application built with .NET 8.0 and WinForms that automatically sends commands to Claude Code CLI every 5 hours.

## File Structure

```
ClaudeCodeHelper/
│
├── ClaudeCodeHelper.csproj      # Project configuration file
├── Program.cs                    # Application entry point
├── TrayApplicationContext.cs     # Main application logic
├── app.manifest                  # Windows application manifest
│
├── build.bat                     # Build script
├── setup.bat                     # Setup and installation script
│
├── README.md                     # Main documentation
├── QUICKSTART.md                # Quick start guide
├── PROJECT_STRUCTURE.md         # This file
│
├── .gitignore                   # Git ignore rules
│
└── bin/                         # Build output (generated)
    └── Release/
        └── net8.0-windows/
            └── ClaudeCodeHelper.exe
```

## Core Components

### Program.cs
- Entry point for the application
- Sets up WinForms application context
- Initializes the tray application

### TrayApplicationContext.cs
Main application logic containing:

#### Features
1. **System Tray Icon Management**
   - Creates and manages the tray icon
   - Handles tooltip text with session/token info
   - Context menu creation

2. **Timer System**
   - Command timer: Sends "hi" every 5 hours
   - Tooltip update timer: Updates tooltip every 5 seconds

3. **Command Execution**
   - Executes `echo hi | claude` command
   - Captures and parses output for token information
   - Handles errors gracefully

4. **Session Tracking**
   - Tracks session start time
   - Monitors token usage (remaining/total)
   - Persists session data to disk

5. **Windows Integration**
   - Registry management for startup
   - Application data storage in %AppData%
   - Balloon tip notifications

#### Key Methods
- `SendHiCommand()` - Executes the Claude Code command
- `UpdateTooltip()` - Updates tray icon tooltip with session info
- `ParseTokenInfo()` - Extracts token data from command output
- `LoadSessionData()` / `SaveSessionData()` - Persistent storage
- `SetStartupRegistry()` - Windows startup integration

### ClaudeCodeHelper.csproj
.NET project configuration:
- Target: .NET 8.0 Windows
- Output: Windows Executable (WinExe)
- Framework: WinForms

### app.manifest
Windows application manifest:
- Execution level: asInvoker (no admin required)
- DPI awareness: Per-Monitor V2
- Compatibility: Windows 10/11

## Data Storage

### Location
`%AppData%\ClaudeCodeHelper\`

### Files
1. **lastsent.txt**
   - Stores timestamp of last command sent
   - Format: ISO 8601 datetime string

2. **session.txt**
   - Line 1: Session start time (ISO 8601)
   - Line 2: Remaining tokens (integer)
   - Line 3: Total tokens (integer)

## Build Process

### Prerequisites
- .NET 8.0 SDK
- Windows 10 or later

### Build Commands

**Using build script:**
```batch
build.bat
```

**Manual build:**
```batch
dotnet build -c Release
```

**Output:**
```
bin\Release\net8.0-windows\ClaudeCodeHelper.exe
```

## Registry Integration

### Startup Entry
- Key: `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
- Value Name: `ClaudeCodeHelper`
- Value Data: `"C:\path\to\ClaudeCodeHelper.exe"`

## Dependencies

### .NET Framework
- Microsoft.NET.Sdk
- System.Windows.Forms
- System.Drawing
- Microsoft.Win32 (Registry access)

### External Dependencies
- Claude Code CLI (must be in PATH)

## Design Patterns

### Application Context Pattern
- Extends `ApplicationContext` for tray-based application
- No main form, runs entirely in system tray

### Timer-based Architecture
- Non-blocking timers for periodic tasks
- Separate timers for different responsibilities

### Persistent State
- Session data survives application restarts
- Graceful handling of missing/corrupt data

## Extensibility

### Modifying the Interval
[TrayApplicationContext.cs:17](TrayApplicationContext.cs#L17)
```csharp
private readonly TimeSpan _interval = TimeSpan.FromHours(5);
```

### Changing the Command
[TrayApplicationContext.cs:181](TrayApplicationContext.cs#L181)
```csharp
Arguments = "/c echo hi | claude"
```

### Adjusting Tooltip Update Rate
[TrayApplicationContext.cs:102](TrayApplicationContext.cs#L102)
```csharp
period: TimeSpan.FromSeconds(5)
```

## Error Handling

- All timer callbacks wrapped in try-catch
- Graceful degradation when token parsing fails
- Silent failures for non-critical operations
- User notifications for critical errors via balloon tips

## Security Considerations

- Runs with user privileges (no elevation required)
- No network connections
- Only accesses local Claude Code CLI
- Registry access limited to current user hive
- Data stored in user's AppData folder

## Future Enhancement Ideas

1. Configurable interval via settings dialog
2. Multiple command profiles
3. Detailed logging system
4. Statistics dashboard
5. Custom tray icon
6. Notification sound options
7. Integration with Claude API directly
8. Export session history
