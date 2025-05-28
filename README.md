# EasySave

A robust C# file backup application with a modern WPF GUI interface for efficiently managing backup jobs.


## Changelog

For a detailed list of changes, new features, and bug fixes in each version, please see the [CHANGELOG.md](CHANGELOG.md) file.

## Features

- **Modern WPF Interface**: Intuitive Windows-native graphical user interface
- **Multiple Backup Types**: Support for full and differential backups
- **Job Management**: Create, start, stop, pause, resume, and monitor backup jobs
- **Remote Job Management (TCP Server)**: Monitor and control backup jobs remotely via TCP (client application in development).
- **Global Transfer Throttling**: Limit total concurrent file transfer size across all backup jobs to manage bandwidth and system resources.
- **Real-time Progress Tracking**: Visual progress bars and status indicators
- **Comprehensive Logging**: Detailed logging of backup activities, states, and critical errors to daily structured text logs and a dedicated `EasySave33_bugReport.log`.
- **Multi-language Support**: Support for English and French languages
- **File Verification**: Uses XXHash64 algorithm for efficient file comparison
- **Blocked Software Detection**: Prevents backups when specified software is running
- **File Encryption**: Automatic encryption of specified file types using CryptoSoft (automatically downloaded if not present)
- **Priority File Extensions**: Backup files with specific extensions first
- **Continuous Backup**: Automatic monitoring and backup of changes
- **Settings Persistence**: Save and load application settings automatically
- **Enhanced Stability**: Robust global unhandled exception handling.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Windows operating system
- WPF support

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/Nayxooo/better_saving.git
   ```

2. Navigate to the project directory:
   ```bash
   cd better_saving
   ```

3. Build the application:
   ```bash
   dotnet build
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

### Download Pre-built Executable

For convenience, a pre-built `.exe` file is available in the [Releases section](https://github.com/Nayxooo/better_saving/releases). Simply download the latest release and run it directly without needing to build from source.

### Building a Single Executable

To build the application into a single `.exe` file:
```bash
dotnet publish better_saving.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true -o "publish"
```

## Usage

1. **Launch the application**
2. **Create a new backup job** by clicking the "+" button and specifying:
   - Job name
   - Source directory (files to backup)
   - Target directory (backup destination)
   - Backup type (Full or Differential)
3. **Configure settings** (optional):
   - Set blocked software that should prevent backups
   - Configure file extensions for encryption
   - Set priority file extensions for faster backup
   - Choose your preferred language
4. **Start backup jobs** individually or all at once
5. **Monitor progress** with real-time status updates and progress bars
6. **Pause/Resume** jobs as needed
7. **View detailed job information** by clicking on any job

## Interface Features

The modern WPF interface includes:

- **Main Dashboard**: Overview of all backup jobs with status indicators
- **Job Creation**: User-friendly form for creating new backup jobs
- **Job Status View**: Detailed progress and statistics for individual jobs
- **Settings Panel**: Configure application preferences and backup parameters
- **Multi-language Support**: Switch between English and French interfaces

### Job States

- **Working**: Job is actively backing up files
- **Finished**: Job completed successfully
- **Paused**: Job is temporarily halted (can be resumed from the same point)
- **Stopped**: Job is stopped and restarting will begin from the beginning
- **Failed**: Job encountered an error

## Advanced Features

### File Encryption
Configure specific file extensions to be automatically encrypted during backup using the integrated CryptoSoft tool.

### Priority File Extensions
Set file extensions that should be backed up first, ensuring critical files are processed with priority.

### Blocked Software Detection
Prevent backups from running when specified software is active, avoiding potential conflicts.

### Continuous Backup
Jobs automatically monitor for changes and maintain up-to-date backups.

### Remote Job Management
Enable the built-in TCP server to monitor and control backup jobs (start, pause, stop) from a remote client. The server also broadcasts job state updates. (Client application is currently in development).

## Project Structure

- **Models/**: Core data models for backup jobs, logging, hashing, and settings
- **ViewModels/**: MVVM pattern view models for UI logic
- **Views/**: WPF user controls and windows
- **Converters/**: Value converters for UI data binding
- **Assets/**: Icons, fonts, and other resources
- **Resources/**: Localization files for multi-language support

## Technical Details

- **Framework**: .NET 8.0 with WPF
- **Architecture**: MVVM (Model-View-ViewModel) pattern
- **Hashing**: XXHash64 for efficient file comparison
- **Logging**: Structured text-based daily logs (`.log`) with detailed operation tracking and a separate `EasySave33_bugReport.log` for critical errors.
- **Remote Management**: TCP server for remote job control and state broadcasting.
- **Settings**: Persistent JSON configuration files
- **Threading**: Async/await pattern for non-blocking operations

## Configuration Files

- `EasySave33.settings`: Application settings (blocked software, file extensions, language, TCP server state, transfer limits)
- `CryptoSoft.settings`: Encryption configuration (if using encryption features) (automatically created if not present)

## Dependencies

- **SharpVectors.Wpf**: SVG icon rendering
- **System.IO.Hashing**: XXHash64 implementation


## Acknowledgments

- WPF framework for the modern Windows interface
- XXHash64 for high-performance file hashing
- SharpVectors for scalable vector graphics support