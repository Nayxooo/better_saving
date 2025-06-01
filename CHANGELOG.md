# EasySave Changelog

## EasySave [v3.0.4] - 2025-06-01

### Fixed
- **Robustness of Backup Jobs with Encryption (`Models/BackupJob.cs`):**
    - Resolved an issue where backup jobs would prematurely fail or halt if `CryptoSoft.exe` encountered an error during the encryption of a single file. Jobs now log the specific file error and attempt to continue processing the remaining files.

### Changed
- **CryptoSoft Error Diagnostics (`Models/BackupJob.cs`, `Models/Enums.cs`, `Resources/Localization/*.xaml`):**
    - The error key `BackupJobErrorMessageKeys.CryptoSoftExeNotFound` has been consolidated into `BackupJobErrorMessageKeys.CryptoSoftInternalError` for reporting issues with `CryptoSoft.exe`.
    - The corresponding English error message for `CryptoSoftInternalError` has been enhanced to display both the affected file path and the numerical error code from `CryptoSoft.exe`, aiding in diagnostics.
    - French localization for this error message has also been updated.

## EasySave [v3.0.3] - 2025-06-01

### Added
- **Remote Job Control (`Models/Enums.cs`, `Models/TCPServer.cs`):**
    - Introduced `RESUME_JOB` command to the TCP server, enabling remote resumption of paused backup jobs.

### Fixed
- **Job State Handling (`Models/BackupJob.cs`):**
    - Fixed a bug where the `Pause()` method would create an error if the job was already paused before (pause->resume->pause:error).
- **Logging (`Models/logger.cs`):**
    - Removed a redundant diagnostic log call from `UpdateAllJobsState` method.

## EasySave [v3.0.2] - 2025-06-01

### Changed
- **State File Handling (`Models/TCPServer.cs`):**
    - TCPServer now only sends the `state.json` after a request from a client.

### Fixed
- **State File Handling (`Models/TCPServer.cs`):**
    - Ensured that an empty or malformed `state.json` file is gracefully handled by returning an empty JSON array (`[]`) instead of `null` or crashing. This improves robustness when the state file is corrupted, not yet created or simply empty because no jobs have been created yet.
- **Logging (`Models/logger.cs`):**
    - In `UpdateAllJobsState`, added a `SkipNullCheck` parameter (defaulting to `false`). When `true`, this allows `state.json` to be updated even if the job list is empty (e.g., after deleting the last job).
    - Ensured `LogError` is used consistently for logging errors within the `Logger` class itself.
    - Corrected the `LoadJobsState` method to initialize `loadedJobs` at the beginning of the method and return it, ensuring it's not re-scoped inside the `lock`.
- **ViewModel Updates (`ViewModels/BackupListViewModel.cs`):**
    - When removing a job in `RemoveJob`, `_logger.UpdateAllJobsState(SkipNullCheck: true)` is now called to ensure `state.json` can be updated to an empty array if the last job is removed.
- **Navigation (`ViewModels/MainViewModel.cs` & `ViewModels/BackupStatusViewModel.cs`):**
    - Introduced `ForceNullView()` in `MainViewModel` to reliably clear the current detail view.
    - `BackupStatusViewModel.ExecuteDeleteJob` now calls `_mainViewModel.ForceNullView()` after removing a job.

## EasySave [v3.0.1] - 2025-06-01

### Added
- **Localization for BackupJob Messages:**
    - Implemented localization for error and information messages displayed by `BackupJob`.
    - Introduced `BackupJobInfoMessageKeys` and `BackupJobErrorMessageKeys` enums in `Models/Enums.cs` to manage message keys.
    - Added corresponding English translations in `Resources/Localization/Strings.en-US.xaml`.
    - Added corresponding French translations in `Resources/Localization/Strings.fr-FR.xaml`.
    - Added helper methods `GetLocalizedInfoMessage` and `GetLocalizedErrorMessage` in `Models/BackupJob.cs` to retrieve localized strings.

### Changed
- **BackupJob Message Handling (`Models/BackupJob.cs`):**
    - Refactored `BackupJob` to use the new localization system for all user-facing error and information messages, replacing hardcoded strings.

## EasySave [v3.0.0] - 2025-05-31

### Info
This version includes several bug fixes and feature enhancements.

Exited from the preview stage as client is in working state (not yet fully featured).

### Added
- **Job Status View UI:**
    - Added an `infoMessage` textbox in `JobStatusView`.

### Changed
- **TCP Server Performance:**
    - Optimized state.json broadcasting to reduce network traffic.
- **ReadMe Updates:**
    - Updated README with new images of the GUI.
- **Job Loading:**
    - Changed how a job is loaded to include the full progress.


### Fixed
- **Job Execution:** Fixed a bug where jobs would not pause and resume correctly.
- **Logging:** Fixed logging in `BackupJobs.cs`.

## EasySave [v3.0.0-preview] - 2025-05-28

### Info
This version introduces server-side capabilities for remote job management via TCP. The client-side application for remote control is in development.

### Added
- **TCP Server for Remote Job Management:**
    - Integrated a TCP server to allow remote monitoring and control of backup jobs.
    - Supports commands: `PING`, `START_JOB`, `PAUSE_JOB`, `STOP_JOB`.
    - Broadcasts `state.json` updates to connected clients (sensitive path information is excluded).
    - New settings in `SettingsView` to enable/disable the TCP server and view its IPaddress:Port.
- **New Icons:**
    - Added new icon for jobs in `Paused` state.
- **Enhanced Job Control Logic:**
    - Added `IsStopping` property to `Models/BackupJob.cs` to clearly differentiate stop actions from pause actions during task cancellation.
- **Error Logging:**
    - `Models/logger.cs` now includes an `LogError` method to write detailed errors to `logs/EasySave33_bugReport.log`.
    - The `EasySave33_bugReport.log` file is cleared on application startup.
- **Localization:**
    - Added localization strings for TCP server settings in `Resources/Localization/Strings.en-US.xaml` and `Resources/Localization/Strings.fr-FR.xaml`.
- **Enums:**
    - Introduced `RemoteCommands` enum in `Models/Enums.cs` for TCP communication.
- **Settings:**
    - Added `IsTcpServerEnabled` property to `Models/Settings.cs` to persist TCP server state.

### Changed
- **Job Pause/Stop Behavior (`Models/BackupJob.cs`):**
    - `Stop()` method now sets `IsStopping` and `IsPausing` flags; progress and file counts are reset.
    - `Pause()` method now directly cancels the execution token; `CheckCancellationRequested()` handles setting the state to `Paused`.
    - `CheckCancellationRequested()` now distinguishes between stop (sets state to `Stopped`) and pause (sets state to `Paused`), logging the specific action.
    - `ExecuteAsync()` includes more robust checks for `JobStates.Stopped` and refined `OperationCanceledException` handling.
- **ViewModel Architecture & Interactions:**
    - `ViewModels/MainViewModel.cs`: Instantiates and manages the `TCPServer`, passing it to `BackupListViewModel` and `Logger`. Handles TCP server toggling based on settings.
    - `ViewModels/SettingsViewModel.cs`: Manages TCP server enable/disable state, displays server address, and indicates unsaved changes in the view title with an asterisk (`*`).
    - `ViewModels/BackupStatusViewModel.cs`: `ExecutePauseResumeJob` now calls `SelectedJob.Pause()` for cleaner pause initiation.
    - `ViewModels/BackupListViewModel.cs`: Constructor now accepts and passes `TCPServer` to the `Logger`.
- **Logging (`Models/logger.cs`):**
    - Daily log files now use the `.log` extension (previously `.json`) and a new structured text format (e.g., `[{timestamp}] [{jobName}] ...`).
    - `UpdateAllJobsState()` now notifies the `TCPServer` to broadcast `state.json` changes to connected clients.
- **UI/UX:**
    - **Icons:**
        - Updated `Assets/Icons/pause.svg` and `Assets/Icons/play.svg`.
        - Cleaned metadata from `Assets/Icons/settings.svg`.
    - `Views/BackupListView.xaml`: Icons for 'Stopped' and 'Paused' states updated to use the new/modified pause icons. Minor style adjustments for item selection.
    - `Views/SettingsView.xaml`:
        - Added UI elements for TCP server configuration (enable toggle, address display).
        - Improved styling for TextBoxes (`RoundedTextBoxStyle`) and CheckBoxes (`ModernCheckBoxStyle`).
        - Settings title now indicates unsaved changes.

### Fixed
- **Logging Call:** Corrected arguments in a `LogBackupDetails` call within `BackupListViewModel.StartAllJobs()` to ensure all required parameters (including `encryptionExitCode`) are passed when blocked software prevents job startup.

## EasySave [v3.0.0-beta.5] - 2025-05-27

### Info
This version is still in beta due to the lack of Server<->Client remote viewing and control functionality (in development).

### Added
- **Global Transfer Throttling:**
    - Implemented a system to limit the total concurrent file transfer size across all backup jobs. This helps manage bandwidth and system resources.
    - Added a new setting in the "Settings" view ("Maximum Transfer Size (KB)") to configure this global limit. A value of 0 means no limit.
    - Backup jobs will now intelligently pause if initiating a new file transfer would exceed this configured global limit. They will automatically resume when sufficient capacity becomes available.
- **Stop Job Functionality:**
    - Introduced a dedicated "Stop" button in the `BackupStatusView` to explicitly stop an ongoing ('Working') or 'Paused' job. This button is conditionally visible based on the job's state.
    - Implemented the underlying `StopJobCommand` in `BackupStatusViewModel`, which calls the `backupJob.Stop()` method.
- **Enhanced Exception Handling & Stability:**
    - Integrated robust global unhandled exception handlers (`AppDomain.CurrentDomain.UnhandledException` and `DispatcherUnhandledException`).
    - Critical errors are now logged to `logs\EasySave33.bugReport` and via the main application logger, aiding in diagnostics.
    - A user-friendly message box now informs the user about unexpected critical errors, and the application attempts to prevent abrupt crashes.

### Changed
- **UI Styling & Consistency:**
    - All application views (`BackupCreationView`, `BackupListView`, `BackupStatusView`, `SettingsView`) have been updated to consistently use the centralized color scheme defined in `Resources/Colors.xaml`.
    - `Resources/Colors.xaml` was itself updated with new color definitions (e.g., `PrimarySelectedColor`, `DeleteButton` colors, `JobState` colors for various states like Finished, Failed, Stopped, Paused) and modifications to existing ones. Comments were added to clarify OKLCH values for hover/selected states.
    - The `BooleanToColorConverter` was enhanced to dynamically fetch color brushes from application resources, making it more flexible.
- **Backup Job Execution Core Logic:**
    - The `BackupJob.ExecuteAsync` method underwent significant refactoring to improve clarity, reliability, and to seamlessly integrate the new global transfer throttling mechanism.
    - Backup jobs now automatically create the target directory if it does not exist before starting the backup process.
    - Improved internal error message handling and state transitions within individual `BackupJob` instances.
- **ViewModel Architecture & Responsibilities:**
    - The pause/resume/start logic in `BackupStatusViewModel` has been streamlined:
        - The `ExecutePauseResumeJob` method now correctly manages `CancellationTokenSource` for job execution and sets the job state to `Stopped` (previously `Paused`) upon task cancellation (`OperationCanceledException`).
        - Corrected parameters in a `LogBackupDetails` call for scenarios where blocked software prevents job operations.
    - `BackupListViewModel` now provides clearer error feedback to the user if starting all jobs is prevented (e.g., by blocked software).
- **View-Specific UI Enhancements:**
    - **BackupStatusView:**
        - Enhanced the dynamic icon display for the Play/Pause button to more accurately reflect job states ('Working', 'Paused', 'Pausing').
- **Settings Management & Navigation:**
    - `MainViewModel` and `SettingsViewModel` were updated to support the new "Maximum Transfer Size" setting, including loading and saving its value.
    - Navigation within `MainViewModel` (specifically when closing views like Settings) has been improved to correctly return to the previously active view by utilizing a `PreviousView` tracker.
    - The command to open/close the settings view was renamed from `ShowSettingsViewCommand` to `ToggleSettingsViewCommand` in `MainViewModel`, and its usage was updated in `BackupListView.xaml` for improved toggle behavior.
- **Logging Refinements:**
    - Minor improvements to log entry formatting for daily logs.
    - Enhanced robustness in the `Logger` class, including a fallback mechanism to `logger_fallback_error.debug` if writing to the primary log file fails.
    - Job pausing due to exceeding transfer limits is now logged for better traceability.

### Fixed
- Ensured that backup jobs saved in a `Paused` state in `state.json` are correctly loaded back into the `Paused` state upon application startup.

## EasySave [v3.0.0-beta.4] - 2025-05-27

### Added
- Introduced a central file (`Resources/Colors.xaml`) for managing UI colors, ensuring consistent styling.

### Changed
- **Job State Overhaul**:
    - The `Idle` state for jobs has been removed; its functions are now part of the `Stopped` state.
    - The `Paused` state is now used more broadly, especially when jobs are paused manually or blocked by other software.
- **UI Enhancements**:
    - Job list and status views now use the new central color system for item backgrounds and icons, which change based on job state.
    - Icon display logic was updated to match the new job states.
- **Logging Improvement**:
    - Logging backup details is now simpler as timestamps are handled automatically.
- **Backup Job Behavior**:
    - Stopping a backup job now sets its state to `Paused` instead of `Stopped`.
    - Better management of job states during pause, stop, or when software blocks execution.
    - New backup jobs will now start in the `Stopped` state.

### Removed
- The `JobStateToIconConverter.cs` file was removed as it served no purpose (not even passing the Butter).



## EasySave [v3.0.0-beta.3] - 2025-05-23

### Added
- Added file extensions priority functionality.

### Fix
- `CryptoSoft.exe` encryption call.
- Bug preventing job from being stopped after viewing another job status.

## EasySave [v3.0.0-beta.2] - 2025-05-23

### Added
- Added copyable paths for directories

### Fix
- Progress property now uses float for smoother progress bar

## EasySave [v3.0.0-beta.1] - 2025-05-23

### Fix
- Change settings view to be in it's own `.xaml`.

## EasySave [v2.1.0] - 2025-05-23

### Added
- Added settings save functionality (settings are now saved in `EasySave33.settings`).
- Better coloring in the settings view.

## EasySave [v2.0.4] - 2025-05-23

### Added
- add application icon
### Fixed
- fixed logger bug where `state.json` would not be properly updated

## EasySave [v2.0.3] - 2025-05-22

### Fixed
- Fixed a potential crash when starting the `CryptoSoft.exe` process.
- Fixed issue with the error message not displaying on multiples lines.
- Fixed visual bug on the job button in the Backup job list (removed default Windows blue outline and overlay).

## EasySave [v2.0.2] - 2025-05-21

### Fixed
- Corrected logging of blocked software to accurately reflect an empty list.
- Prevented a potential crash in the `EncryptFilesInLogs` method by adding a null check when starting the `CryptoSoft.exe` process.

## EasySave [v2.0.1] - 2025-05-21

### Fixed
- Small UI and internal file naming fixes.

## EasySave [v2.0.0] - 2025-05-20

### Added
- Replace the Console application with a WPF application.

## EasySave [v1.0.1] - 2025-05-15

Console application with a simple menu to create and manage backup jobs.