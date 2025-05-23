# CryptoSoft

A lightweight file encryption and decryption utility built in C#.

## Overview

CryptoSoft is a simple command-line utility that provides secure file encryption and decryption using XOR cipher. The application automatically detects whether a file needs to be encrypted or decrypted based on the presence of a header signature.

## Features

- **Simple Command-Line Interface**: Easy to use and integrate into scripts or other applications
- **Automatic Operation Detection**: Automatically determines whether to encrypt or decrypt based on file headers
- **Performance Metrics**: Reports processing time in milliseconds
- **Configuration-Based Key Management**: Reads encryption keys from a JSON configuration file
- **Error Handling**: Comprehensive error handling with specific exit codes

## How It Works

CryptoSoft uses a simple but effective XOR operation with a secret key to transform file contents:
- When encrypting, it adds a `[CRYPT]` header to the file
- When decrypting, it detects the header, removes it, and restores the original file

## Getting Started

### Prerequisites

- .NET SDK (version compatible with your build)
- Windows operating system

### Installation

1. Clone the repository or download the source code
2. Build the application:
   ```
   dotnet build
   ```

### Usage

```
CryptoSoft.exe <filepath> <configpath>
```

Parameters:
- `<filepath>`: Path to the file you want to encrypt or decrypt
- `<configpath>`: Path to the configuration file (JSON) containing the encryption key

Example:
```
CryptoSoft.exe C:\path\to\secret.txt C:\path\to\CryptoSoft.settings
```

### Configuration

Create a JSON configuration file with the following structure:

```json
{
  "EncryptionKey": "YourSecretKey"
}
```

Notes:
- The key must be at least 8 characters long
- Use a strong, unique key for better security

## Exit Codes

- **Positive Value**: Success (value represents processing time in milliseconds)
- **-1**: Invalid argument count
- **-2**: Source file not found
- **-3**: Configuration file not found
- **-4**: Invalid encryption key
- **-5**: Error during file processing
- **-99**: Unexpected error

## Technical Details

- **Encryption Method**: XOR cipher with key rotation
- **Header Signature**: Files are marked with a `[CRYPT]` header when encrypted
- **File Processing**: Files are processed in-memory for efficient operation

## Security Considerations

- XOR encryption is not suitable for highly sensitive data
- The security depends on keeping the encryption key confidential
- This tool is designed for basic encryption needs, not for military-grade security