# RdmpExtensions
A collection of non-core RDMP functionality.
This includes:

* Automation Plugins

    Allows Automation of extractions

* Interactive Plugins

    Contains the DeAnonymise functionality to deanonymise against a cohort

* Python

    Allows Python scripts to be run as data providers

* Release Plugin

    Allows releases from remote RDMP instances
    
* StatsScriptsExecution Plugin
    
    Allows R scripts to be run from RDMP

## Building

Before Building, ensure the version number is correct within the rdmpextension.nuspec and SharedAssemblyInfo.cs
 file.

You can build this plugin ready for upload to an RDMP instance using:

```bash
dotnet publish -p:DebugType=embedded -p:GenerateDocumentation=false Plugin/windows/windows.csproj -c Release -o p/windows
dotnet publish -p:DebugType=embedded -p:GenerateDocumentation=false Plugin/main/main.csproj -c Release -o p/main
7z a -tzip Rdmp.Extensions.Plugin.6.2.1.rdmp rdmpextension.nuspec p
dotnet run --project RDMP/Tools/rdmp/rdmp.csproj -c Release -- pack -p --file Rdmp.Extensions.Plugin.6.2.1.rdmp --dir yaml
```

Once built you will have a file called ` Rdmp.Extensions.Plugin.6.2.1.rdmp`. The last step (with the '-p' switch to the pack command) strips out all the duplicated DLLs which are already provided within RDMP.

Upload it to RDMP using

```bash
./rdmp pack -f Z:\Repos\RdmpExtensions\Rdmp.Extensions.Plugin.6.2.1.rdmp
```
_Upload into RDMP. Or use the gui client 'Plugins' node under the Tables(Advanced) toolbar button_
