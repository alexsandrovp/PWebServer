git clean -dfx
$numbers = Get-Content ./version.txt
$version = $numbers -join '.'
$text = @"
using System.Reflection;
[assembly: AssemblyVersion("$version")]
[assembly: AssemblyFileVersion("$version")]
"@;

$text | Set-Content ./AssemblyInfoVer.cs -Encoding UTF8

(Get-Content ./PWebServer/PWebServer.psd1) -replace '^(\s*ModuleVersion\s*?=\s*?)(\S+)(\s*)$', ('${1}' +"'$version'" + '$3') | Set-Content ./PWebServer/PWebServer.psd1 -Encoding UTF8

nuget restore ./PWebServer.sln
msbuild ./PWebServer.sln /p:Configuration=Release
del ./PWebServer/bin/Release/net5.0/PWebServer.deps.json
del ./PWebServer/bin/Release/net5.0/ref -rec -for
move ./PWebServer/bin/Release/net5.0/PWebServer.pdb ./PWebServer.pdb
move ./PWebServer/bin/Release/net5.0 "./PWebServer/bin/Release/PWebServer"
. ./installer/PSModuleInstaller.ps1
New-ModuleInstaller -ModuleSource ./PWebServer/bin/Release/PWebServer