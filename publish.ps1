Write-Output "Building..."
dotnet build -c Release --force
Write-Output "Building for win-x64..."
dotnet build -c Release -r win-x64 --force
dotnet publish -c Release -r win-x64 --force
Write-Output "Building for win-x86..."
dotnet build -c Release -r win-x86 --force
dotnet publish -c Release -r win-x86 --force
Write-Output "Building for linux-x64..."
dotnet build -c Release -r linux-x64 --force
dotnet publish -c Release -r linux-x64 --force
Write-Output "Building for osx-x64..."
dotnet build -c Release -r osx-x64 --force
dotnet publish -c Release -r osx-x64 --force

Write-Output "Packing..."
dotnet pack -c Release --force
Write-Output "Packing for win-x64..."
Compress-Archive -Force -Path "./src/gop/bin/Release/netcoreapp2.2/win-x64/publish/*" -DestinationPath "./src/gop/bin/Release/builded/win-x64" -ErrorAction Stop
Write-Output "Packing for win-x86..."
Compress-Archive -Force -Path "./src/gop/bin/Release/netcoreapp2.2/win-x86/publish/*" -DestinationPath "./src/gop/bin/Release/builded/win-x86" -ErrorAction Stop
Write-Output "Packing for osx-x64..."
Compress-Archive -Force -Path "./src/gop/bin/Release/netcoreapp2.2/osx-x64/publish/*" -DestinationPath "./src/gop/bin/Release/builded/osx-x64" -ErrorAction Stop
Write-Output "Packing for linux-x64..."
Compress-Archive -Force -Path "./src/gop/bin/Release/netcoreapp2.2/linux-x64/publish/*" -DestinationPath "./src/gop/bin/Release/builded/linux-x64" -ErrorAction Stop
