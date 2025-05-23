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