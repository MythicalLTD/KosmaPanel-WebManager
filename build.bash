#!/bin/bash

runtimes=("linux-x64" "linux-x86" "linux-arm64" "linux-arm")
rm -r bin/Release
rm -r obj/
rm ~/KosmaPanel-WebManager/KosmaPanelARM32
rm ~/KosmaPanel-WebManager/KosmaPanelARM64
rm ~/KosmaPanel-WebManager/KosmaPanel64

for runtime in "${runtimes[@]}"; do
    echo "Publishing for runtime: $runtime"
    dotnet clean
    dotnet restore
    dotnet publish -c Release -r "$runtime" --self-contained true /p:PublishSingleFile=true -p:Version=1.0.0.1 
    echo "----------------------------------"
done
mv ~/KosmaPanel-WebManager/bin/Release/net7.0/linux-arm/publish/KosmaPanel ~/KosmaPanel-WebManager/
mv KosmaPanel KosmaPanelARM32
mv ~/KosmaPanel-WebManager/bin/Release/net7.0/linux-arm64/publish/KosmaPanel ~/KosmaPanel-WebManager/
mv KosmaPanel KosmaPanelARM64
mv ~/KosmaPanel-WebManager/bin/Release/net7.0/linux-x64/publish/KosmaPanel ~/KosmaPanel-WebManager/
mv KosmaPanel KosmaPanel64