Set-Location gop
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r win-x86
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r osx-x64
