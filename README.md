Command to build into a signle `.exe` file
```bash
dotnet publish better_saving.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true -o "publish"
```