# EasySave - Code Overview

## Architecture

EasySave is a C# console application that implements a file backup system using the Model-View-Controller (MVC) pattern:

1. **Models**: Represent data structures and business logic
2. **Views**: Handle user interface through Terminal.Gui
3. **Controllers**: Coordinate interactions between models and views

## Key Components

### Controller (Controllers/Controller.cs)
- Central coordinator for the application
- Manages backup jobs (create, start, stop, delete)
- Handles job state persistence using JSON
- Tracks running jobs with cancellation tokens
- Provides methods to start individual jobs or all jobs at once
- Handles loading of previously configured jobs from state.json

### BackupJob (Models/BackupJob.cs)
- Represents a backup task with properties like:
  - Source and target directories
  - Type (Full or Differential)
  - State (Working, Finished, Stopped, Failed, Idle)
  - Progress tracking with file counts and sizes
  - Error message storage
- Implements execution logic for copying files
- Uses property observers to automatically update state in logger
- Handles asynchronous execution with cancellation support

### ConsoleInterface (Views/Console.cs)
- Terminal-based GUI using Terminal.Gui library
- Provides interactive menus for job management
- Supports multiple languages (English and French)
- Displays job status and progress with visual indicators
- Features dark theme with customized color schemes
- Implements responsive dialog windows for various operations
- Provides real-time job monitoring with status updates

### Logger (Models/Logger.cs)
- Records application events and job status
- Writes logs to JSON files with daily rotation
- Maintains state.json for persistent job information
- Implements thread-safe logging to handle concurrent operations

### Backup (Models/Backup.cs)
- Handles the actual file copying operations
- Measures and returns the time taken for file transfers
- Provides error handling for file operations

### Hashing (Models/hash.cs)
- Provides efficient file comparison functionality using XXHash64 algorithm
- XXHash64 is a non-cryptographic, extremely fast hashing algorithm:
  - Designed for speed rather than cryptographic security
  - Uses 64-bit arithmetic operations (multiplication by prime numbers, rotations, XOR)
  - Processes data in 32-byte blocks with excellent distribution properties
  - Achieves superior performance compared to MD5/SHA algorithms for checksumming
- Implementation uses 8MB buffer chunks for optimal memory usage
- Enables fast differential backup operations by efficiently identifying changed files

## Backup Types

### Full Backup
- Copies all files from source to destination
- Replaces existing files regardless of modification status
- Simple but potentially time and storage intensive

### Differential Backup
- Copies only files that have changed since the last backup
- Uses XXHash64 to efficiently compare source and destination files
- More efficient for frequent backups of large directories
- Requires the destination directory to contain previous backup files

## User Interface

The Terminal.Gui-based interface provides:

- **Main Menu**: Central hub for accessing all functions
- **Language Selection**: Toggle between English and French interfaces
- **Job Creation**: Form-based interface for defining new backup jobs
- **Job Listing**: Table view of all configured jobs with status information
- **Job Detail View**: Comprehensive information about a specific job
- **Progress Tracking**: Visual progress bars showing completion percentage
- **Status Indicators**: Color-coded indicators for job states (Running, Finished, Failed, etc.)

## Flow of Execution

1. Program starts in main.cs, initializing the Controller
2. Controller loads existing jobs from state.json
3. Controller initializes the ConsoleInterface
4. User interacts with the interface to:
   - Create backup jobs
   - Start/stop jobs
   - Monitor job progress
5. When a job is started:
   - Files are enumerated from the source directory
   - For differential backups, only changed files are copied (using hash comparison)
   - Progress is tracked and displayed in the UI
   - Results are logged to files
6. Jobs can be monitored, stopped, or deleted through the interface
7. Application state is persisted to state.json for recovery on restart

## Technical Implementation

- Asynchronous operations for non-blocking UI
- Cancellation tokens for stopping running jobs
- XxHash64 hashing for efficient file comparison in differential backups
- JSON for configuration and logging
- Thread-safe operations for concurrent job execution
- Real-time progress monitoring with UI updates
- Error handling with detailed error messages
- Multi-language support through string dictionaries
- Custom color themes for improved terminal visualization
