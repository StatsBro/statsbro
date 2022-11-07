cd src/StatsBro.Processor.Console
dotnet publish -c Release
cd bin/Release/net6.0

powershell Compress-Archive -Path .\publish -DestinationPath ..\..\StatsBro-console.zip -Force

explorer ..\..\