build:
	dotnet build -c Release
	cp ./bin/Release/netstandard2.1/DeathMessages.dll ~/U3DS/Servers/OpenModTest/OpenMod/plugins/DeathMessages.dll
