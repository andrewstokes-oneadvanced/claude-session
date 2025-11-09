# Claude Code Helper

A .NET Windows application that automatically sends a "hi" command to Claude Code every 5 hours.

## Features

- **Automatic Command Sending**: Sends "hi" to Claude Code every 5 hours
- **System Tray Application**: Runs quietly in the background in the system tray
- **Windows Startup**: Automatically starts with Windows
- **Manual Control**: Right-click menu to send commands immediately or exit the application
- **Status Tracking**: Shows when the last command was sent
- **Session Time Display**: Hover over the tray icon to see current session time (time since first "hi" sent)
- **Token Monitoring**: Displays remaining tokens for the current Claude session in the tooltip
- **Session Reset**: Manually reset the session timer and token counter

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime or SDK
- Claude Code CLI installed and accessible via command line

## Building the Application

### Option 1: Using the build script (Recommended)
1. Make sure you have .NET 8.0 SDK installed
2. Double-click `build.bat` in the project directory
3. The build script will compile the application and show you the executable location

### Option 2: Manual build
1. Make sure you have .NET 8.0 SDK installed
2. Open a command prompt in the project directory
3. Run:
   ```
   dotnet build -c Release
   ```

## Running the Application

After building, you can run the executable:
```
.\bin\Release\net8.0-windows\ClaudeCodeHelper.exe
```

The application will minimize to the system tray immediately.

## Usage

1. **First Run**: The application will automatically add itself to Windows startup
2. **System Tray**: Look for the application icon in the system tray (bottom-right corner)
3. **Tooltip**: Hover over the tray icon to see:
   - Current session duration
   - Remaining tokens / Total tokens for the Claude session
4. **Right-Click Menu**:
   - **Send 'hi' Now**: Manually trigger sending the command
   - **Last Sent**: Shows when the command was last sent
   - **Reset Session**: Reset the session timer and token counter
   - **Run at Windows Startup**: Toggle automatic startup with Windows
   - **Exit**: Close the application
5. **Double-Click**: Double-click the tray icon to see application status

## How It Works

The application uses a timer to execute a command every 5 hours:
```
echo hi | claude
```

This pipes the "hi" text to the Claude Code CLI, simulating user input.

### Session Tracking

- **Session Timer**: Starts tracking time from the first "hi" command sent
- **Token Monitoring**: Attempts to parse token usage information from Claude Code's output
- **Persistent Storage**: Session data is saved to `%AppData%\ClaudeCodeHelper\session.txt`
- **Tooltip Updates**: The tray icon tooltip updates every 5 seconds with current session info

## Customization

To change the interval or command:

1. Open [TrayApplicationContext.cs](TrayApplicationContext.cs)
2. Modify the `_interval` field (line 15) to change the time interval
3. Modify the command in the `SendHiCommand()` method (line 85-92)

## Troubleshooting

- **Command not sending**: Ensure Claude Code CLI is installed and the `claude` command works in Command Prompt
- **Not starting with Windows**: Run the application as Administrator once to set registry keys
- **Icon not visible**: Check your system tray overflow area

## Files

- [ClaudeCodeHelper.csproj](ClaudeCodeHelper.csproj): Project configuration
- [Program.cs](Program.cs): Application entry point
- [TrayApplicationContext.cs](TrayApplicationContext.cs): Main application logic
- [app.manifest](app.manifest): Windows application manifest

## License

Free to use and modify as needed.
