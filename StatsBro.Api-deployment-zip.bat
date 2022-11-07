cd src/StatsBro
dotnet publish -c Release
cd bin/Release/net6.0

powershell Compress-Archive -Path .\publish -DestinationPath ..\..\StatsBro-api.zip -Force

explorer ..\..\