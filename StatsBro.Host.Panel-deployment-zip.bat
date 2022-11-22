cd src/StatsBro.Host.Panel
dotnet publish -p:PublishSingleFile=true -r win-x64 -c Release
cd bin/Release/net6.0/win-x64

powershell Compress-Archive -Path .\publish -DestinationPath ..\..\StatsBro.Host.Panel.zip -Force

explorer ..\..\