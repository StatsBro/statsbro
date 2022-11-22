cd src/StatsBro.Service.Processor
dotnet publish -p:PublishSingleFile=true -r win-x64 -c Release
cd bin/Release/net6.0/win-x64

powershell Compress-Archive -Path .\publish -DestinationPath ..\..\StatsBro.Servce.Processor.zip -Force

explorer ..\..\