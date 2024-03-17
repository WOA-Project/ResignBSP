@echo off

msbuild /m /t:restore,resignbsp:publish /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:PublishDir=%CD%\publish\artifacts\win-x64\CLI /p:PublishSingleFile=true /p:PublishTrimmed=false /p:IncludeNativeLibrariesForSelfExtract=true /p:Configuration=Release ResignBSP.sln