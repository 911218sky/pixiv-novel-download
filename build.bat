@REM dotnet publish -c Release -r win-x64 ^
@REM   --self-contained true ^
@REM   -p:PublishSingleFile=true ^
@REM   -p:PublishTrimmed=false ^
@REM   -p:EnableCompressionInSingleFile=true ^
@REM   -p:IncludeNativeLibrariesForSelfExtract=true ^
@REM   -p:DebugType=none -p:DebugSymbols=false
@REM   -o publish

@REM 非自含模式
@REM dotnet publish -c Release --self-contained false ^
@REM   -p:PublishSingleFile=false ^
@REM   -p:DebugType=none -p:DebugSymbols=false

@REM 自含模式(單檔)
dotnet publish -c Release -r win-x64 --self-contained false ^
  -p:PublishSingleFile=true ^
  -p:EnableCompressionInSingleFile=false ^
  -p:DebugType=none -p:DebugSymbols=false ^
  -o publish
