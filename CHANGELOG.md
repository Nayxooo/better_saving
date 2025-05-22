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
