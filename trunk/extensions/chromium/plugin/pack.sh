dotnet build
dotnet run --project ../../../sdk/tools/logjoint.plugintool pack bin/Debug/net10.0/manifest.xml bin/chromium.zip $1
dotnet run --project ../../../sdk/tools/logjoint.plugintool test bin/chromium.zip ../../../platforms/osx/bin/debug/logjoint.app/Contents/MonoBundle/ --filter=*