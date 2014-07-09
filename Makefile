
all: release

release: bin/Release/Prangster.exe

debug: bin/Debug/Prangster.exe

bin/Release/Prangster.exe:
	xbuild /p:Configuration=Release Prangster.csproj

bin/Debug/Prangster.exe:
	xbuild /p:Configuration=Debug Prangster.csproj

clean:
	$(RM) -r bin
