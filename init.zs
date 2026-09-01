# Publish for Linux x64 (Produces a standalone executable without .NET dependency)
dotnet publish -c Release -r linux-x64 --self-contained

# Publish for Windows x64
dotnet publish -c Release -r win-x64 --self-contained

# Publish for macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained
