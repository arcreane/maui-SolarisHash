# SolarisHash - MAUI Application

A cross-platform hash generation application built with .NET MAUI that provides secure hash computation for files and text.

## Features

- **Multiple Hash Algorithms**: Support for MD5, SHA1, SHA256, SHA384, and SHA512
- **File Hashing**: Generate hashes for files of any size
- **Text Hashing**: Compute hashes for text input
- **Cross-Platform**: Runs on Windows, macOS, iOS, and Android
- **Modern UI**: Clean and intuitive user interface

## Prerequisites

- .NET 7.0 or later
- Visual Studio 2022 17.3+ or Visual Studio Code with C# extension
- For mobile development:
  - Android SDK (for Android targets)
  - Xcode (for iOS/macOS targets, macOS only)

## Installation

1. Clone the repository:
```bash
git clone https://github.com/charv/maui-SolarisHash.git
cd maui-SolarisHash/MyApp
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the application:
```bash
dotnet build
```

## Running the Application

### Windows
```bash
dotnet run --framework net7.0-windows10.0.19041.0
```

### Android
```bash
dotnet run --framework net7.0-android
```

### iOS (macOS only)
```bash
dotnet run --framework net7.0-ios
```

### macOS
```bash
dotnet run --framework net7.0-maccatalyst
```

## Usage

1. **Text Hashing**:
   - Enter your text in the input field
   - Select the desired hash algorithm
   - Click "Generate Hash" to compute the hash

2. **File Hashing**:
   - Click "Select File" to choose a file
   - Select the hash algorithm
   - The hash will be computed automatically

3. **Copy Results**:
   - Click on any generated hash to copy it to clipboard

## Project Structure

```
MyApp/
├── Platforms/          # Platform-specific code
├── Resources/          # App resources (images, fonts, etc.)
├── Views/             # XAML pages and views
├── ViewModels/        # MVVM view models
├── Models/            # Data models
├── Services/          # Business logic and services
├── App.xaml           # Application configuration
├── AppShell.xaml      # App shell navigation
└── MauiProgram.cs     # App startup configuration
```

## Technologies Used

- **.NET MAUI**: Cross-platform framework
- **MVVM Pattern**: Model-View-ViewModel architecture
- **System.Security.Cryptography**: Hash computation
- **CommunityToolkit.Mvvm**: MVVM helpers

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Security Notes

- This application performs hash calculations locally on your device
- No data is transmitted to external servers
- Hash algorithms are implemented using .NET's built-in cryptography libraries

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

If you encounter any issues or have questions:
- Open an issue on GitHub
- Check the [Wiki](../../wiki) for additional documentation

## Changelog

### Version 1.0.0
- Initial release
- Support for MD5, SHA1, SHA256, SHA384, SHA512
- File and text hashing capabilities
- Cross-platform support

---

**Note**: This application is for educational and utility purposes. Always use appropriate hash algorithms for your security requirements.
