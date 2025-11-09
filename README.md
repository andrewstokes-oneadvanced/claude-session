# Claude CLI Session Manager

A comprehensive .NET Windows application that automatically maintains active Claude CLI sessions by sending configurable messages at scheduled intervals.

## ✨ Features

### **Core Functionality**
- **🔄 Automatic Message Sending**: Configurable messages sent at customizable intervals (default: every 5 hours)
- **💬 Custom Messages**: Send any message you want (not just "hi") - perfect for "continue", "status", or custom prompts
- **⏰ Smart Scheduling**: Set specific send times or use automatic intervals based on Claude's session reset times
- **🖥️ CLI Window Management**: Launch and manage multiple Claude CLI windows automatically

### **User Interface**
- **🥷 System Tray Application**: Runs quietly in the background with a ninja-themed icon
- **📊 Real-time Status**: Hover tooltips show next send time, current message, and last activity
- **🎛️ Rich Context Menu**: Comprehensive right-click menu with helpful tooltips for all options
- **🎨 Organized Layout**: Clean, intuitive configuration dialogs with proper spacing

### **Configuration Options**
- **⚙️ Claude CLI Setup**: Configure your Claude CLI command with built-in testing
- **💭 Message Configuration**: Customize the message sent to Claude CLI windows
- **📁 Working Directory**: Set where Claude CLI windows start (project directory, etc.)
- **⏰ Schedule Management**: Configure intervals and specific send times
- **🚀 Startup Behavior**: Auto-launch CLI windows and Windows startup integration

### **Advanced Features**
- **🔍 Smart Window Detection**: Automatically finds Claude CLI windows across different terminal types
- **💾 Persistent Settings**: All configurations saved and restored between sessions
- **🕐 Flexible Timing**: Support for both relative intervals and absolute time scheduling
- **📈 Session Tracking**: Monitors session duration and automatically schedules next messages
- **⚠️ Error Handling**: Robust error handling with user-friendly notifications

## 🎯 Requirements

- Windows 10 or later
- .NET 8.0 Runtime or SDK
- Claude CLI installed and accessible (configurable path)

## 🚀 Quick Start

### Option 1: Using the build script (Recommended)
1. Ensure .NET 8.0 SDK is installed
2. Double-click `build.bat` in the project directory
3. Run the generated executable from the output location

### Option 2: Manual build
```bash
dotnet build -c Release
```

### Option 3: Using setup script
1. Run `setup.bat` for automated setup and configuration
2. Follow the interactive prompts

## 📖 Usage

### **Initial Setup**
1. **First Launch**: Application starts in system tray
2. **Configure CLI**: Right-click → "⚙️ Configure Claude CLI" to set your Claude command
3. **Set Message**: Right-click → "💭 Configure Message" to customize what gets sent
4. **Schedule**: Right-click → "⏰ Configure Schedule" to set timing preferences

### **System Tray Menu**
- **🖥️ Launch New Claude CLI Window**: Opens a new CLI session
- **💬 Send Message to all CLI Windows**: Immediately send your configured message
- **📄 View Last Response**: See details about the last command execution
- **📊 Last Sent** / **⏱️ Next Send**: Status information (read-only)

### **Configuration Options**
- **⚙️ Configure Claude CLI**: Set up CLI command with test functionality
- **💭 Configure Message**: Customize message (default: "hi")
- **📁 Configure Working Directory**: Set starting directory for CLI windows
- **⏰ Configure Schedule**: Set intervals and specific send times

### **Startup Options**
- **🚀 Auto-launch CLI on Startup**: Launch CLI when app starts
- **🔧 Run at Windows Startup**: Start with Windows

## 🔧 How It Works

### **Message Delivery**
The application automatically sends your configured message to all detected Claude CLI windows:
```bash
# Example: sending "continue" message
echo continue | claude
```

### **Smart Window Detection**
Automatically detects Claude CLI running in various terminals:
- Command Prompt
- PowerShell
- Windows Terminal
- ConEmu/Cmder
- Git Bash
- And more!

### **Configuration Storage**
- **CLI Settings**: `%AppData%\ClaudeCodeHelper\cli_config.txt`
- **Session Data**: `%AppData%\ClaudeCodeHelper\session.txt`  
- **Last Activity**: `%AppData%\ClaudeCodeHelper\lastsent.txt`

### **Scheduling Logic**
1. **Automatic**: Uses configured duration (default 5 hours) from last send
2. **Specific Times**: Override with exact times (e.g., 2:00 PM daily)
3. **Claude Integration**: Attempts to parse Claude's session reset times from responses

## ⚙️ Configuration Examples

### **Custom Messages**
- `"hi"` - Simple greeting (default)
- `"continue"` - Resume previous conversation
- `"status"` - Check Claude's current status
- `"What's next?"` - Custom prompt

### **Schedule Scenarios**
- **Every 4 hours**: Set duration to 4.0 hours
- **Daily at 2 PM**: Configure specific time override
- **Workdays only**: Manual scheduling as needed

### **CLI Commands**
- `claude` - Standard Claude CLI
- `claude-3` - Specific Claude version
- `python claude_wrapper.py` - Custom wrapper script
- `C:\Tools\claude\claude.exe` - Full path to executable

## 🔧 Customization

For advanced customization, edit [TrayApplicationContext.cs](TrayApplicationContext.cs):

### **Default Values**
```csharp
private TimeSpan _sessionDuration = TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(1));
private string _messageToSend = "hi";
private string _claudeCliCommand = "claude";
```

### **Terminal Detection**
Modify `FindClaudeCliWindows()` method to support additional terminal types.

## 🐛 Troubleshooting

### **Common Issues**
- **No CLI windows detected**: Ensure Claude CLI is running and window titles contain "Command Prompt", "PowerShell", etc.
- **Messages not sending**: Check CLI configuration and test using the built-in test button
- **Application not starting**: Run as Administrator once to set Windows startup registry keys
- **Wrong working directory**: Configure the working directory setting for your projects

### **Debug Information**
- Check "📄 View Last Response" for detailed execution information
- Hover over tray icon for current status
- Watch for balloon tip notifications

## 📁 Project Structure

```
ClaudeCodeHelper/
├── ClaudeCodeHelper.csproj    # Project configuration
├── Program.cs                 # Application entry point  
├── TrayApplicationContext.cs  # Main application logic
├── app.manifest              # Windows application manifest
├── build.bat                 # Build automation script
├── setup.bat                 # Interactive setup script
└── Documentation/
    ├── README.md             # This file
    ├── QUICKSTART.md         # Quick start guide
    ├── USER_GUIDE.md         # Detailed user guide
    ├── OVERVIEW.md           # Technical overview
    └── PROJECT_STRUCTURE.md  # Project organization
```

## 🤝 Contributing

This project is open for contributions! Feel free to:
- Report bugs via GitHub Issues
- Suggest new features
- Submit pull requests
- Improve documentation

## 📜 License

Free to use and modify as needed. No warranty provided.

---

**💡 Tip**: For the best experience, configure your message and schedule preferences on first use, then let the application run automatically in the background!
