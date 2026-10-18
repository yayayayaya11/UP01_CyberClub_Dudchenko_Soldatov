$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler=Join-Path $framework 'csc.exe'
$refs=@('System.dll','System.Core.dll','System.Data.dll','System.Configuration.dll','System.Xml.dll','System.Xaml.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll') | ForEach-Object { '/reference:'+(Join-Path $framework $_) }
$refs+=@('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/reference:'+(Join-Path $framework "WPF\$_") }
$ef=Join-Path $PSScriptRoot 'packages\EntityFramework.6.4.4\lib\net45'
$refs+=@('EntityFramework.dll','EntityFramework.SqlServer.dll') | ForEach-Object { '/reference:'+(Join-Path $ef $_) }
[xml]$edmx=Get-Content -LiteralPath 'Models\CyberClubModel.edmx' -Raw -Encoding UTF8
$resources=@()
foreach($item in @(@('csdl',$edmx.Edmx.Runtime.ConceptualModels.Schema),@('ssdl',$edmx.Edmx.Runtime.StorageModels.Schema),@('msl',$edmx.Edmx.Runtime.Mappings.Mapping))){
 $path=Join-Path $PSScriptRoot ('Models\CyberClubModel.'+$item[0])
 [IO.File]::WriteAllText($path,$item[1].OuterXml,[Text.UTF8Encoding]::new($false))
 $resources+='/resource:'+$path+',Models.CyberClubModel.'+$item[0]
}
$sources=@(Get-ChildItem -LiteralPath src,Models -Filter '*.cs' | ForEach-Object {$_.FullName})
& $compiler /nologo /target:winexe /platform:anycpu /codepage:65001 /main:CyberClub.Program /out:CyberClub.exe /win32icon:res/club.ico $refs $resources $sources
if($LASTEXITCODE -ne 0){throw 'Application build failed'}
& $compiler /nologo /target:winexe /platform:anycpu /codepage:65001 /main:CyberClub.VerificationEntry /out:Verify.exe $refs $resources $sources (Join-Path $PSScriptRoot 'tests\Verification.cs')
if($LASTEXITCODE -ne 0){throw 'Verification build failed'}
Copy-Item -LiteralPath App.config -Destination CyberClub.exe.config -Force
Copy-Item -LiteralPath App.config -Destination Verify.exe.config -Force
Copy-Item -LiteralPath (Join-Path $ef 'EntityFramework.dll'),(Join-Path $ef 'EntityFramework.SqlServer.dll') -Destination $PSScriptRoot -Force
Write-Output 'Build completed: CyberClub.exe and Verify.exe'
