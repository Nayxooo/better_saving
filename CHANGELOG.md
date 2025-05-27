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