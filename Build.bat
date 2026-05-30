@echo off
cd "IcoGenerator"
dotnet publish .\IcoGenerator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish\win-x64
cd "../IcoGenerator Installer"
dotnet build "IcoGenerator Installer.wixproj" -c Release
copy /b "bin\x64\Release\IcoGenerator_Installer.msi" "..\Ico Generator.msi"
pause