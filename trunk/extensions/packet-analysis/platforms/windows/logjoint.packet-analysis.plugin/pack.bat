dotnet build
dotnet run --project ..\..\..\..\..\sdk\tools\logjoint.plugintool pack bin\Debug\manifest.xml bin\packet-analysis.zip %1
dotnet run --project ..\..\..\..\..\sdk\tools\logjoint.plugintool test bin\packet-analysis.zip ..\..\..\..\..\platforms\windows\bin\debug --filter=*