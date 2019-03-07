Write-Output "Building..."
Write-Output "Building for win-x64..."
dotnet publish -c Release -r win-x64
Write-Output "Building for win-x86..."
dotnet publish -c Release -r win-x86
Write-Output "Building for linux-x64..."
dotnet publish -c Release -r linux-x64
Write-Output "Building for osx-x64..."
dotnet publish -c Release -r osx-x64

Write-Output "Packing..."
dotnet build -c Release
dotnet pack -c Release
Write-Output "Packing for win-x64..."
Compress-Archive -Force -Path "./src/gop/bin/Release/netcoreapp2.2/win-x64/publish/*" -DestinationPath "./src/gop/bin/Release/builded/win-x64" -ErrorAction Stop
Write-Output "Packing for win-x86..."
Compress-Archive -Force -Path "./src/gop/bin/Release/netcoreapp2.2/win-x86/publish/*" -DestinationPath "./src/gop/bin/Release/builded/win-x86" -ErrorAction Stop
Write-Output "Packing for osx-x64..."
Compress-Archive -Force -Path "./src/gop/bin/Release/netcoreapp2.2/osx-x64/publish/*" -DestinationPath "./src/gop/bin/Release/builded/osx-x64" -ErrorAction Stop
Write-Output "Packing for linux-x64..."
Compress-Archive -Force -Path "./src/gop/bin/Release/netcoreapp2.2/linux-x64/publish/*" -DestinationPath "./src/gop/bin/Release/builded/linux-x64" -ErrorAction Stop
