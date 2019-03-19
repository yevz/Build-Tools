<#
.SYNOPSIS
This is a Powershell script to bootstrap a Cake.Frosting build.
.DESCRIPTION
This Powershell script will download NuGet if missing, restore NuGet tools (including Cake)
and execute your Cake build script with the parameters you provide.
.PARAMETER Target
The build script target to run.
.PARAMETER Configuration
The build configuration to use.
.PARAMETER Verbosity
Specifies the amount of information to be displayed.
.PARAMETER WhatIf
Performs a dry run of the build script.
No tasks will be executed.
.PARAMETER ScriptArgs
Remaining arguments are added here.
.LINK
https://github.com/cake-build/frosting
#>

[CmdletBinding()]
Param(
    [string]$Target = "Default",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity = "Normal",
    [switch]$WhatIf,
    [string]$NugetDefaultPushSourceApiKey,
    [string]$NugetDefaultPushSourceUrl,
    [string]$NugetConfigPath = "Nuget.config",
    [string]$SolutionFilePath,
	[string]$OctopusUrl,
	[string]$OctopusApiKey,
	[string]$OctopusProject,
	[string]$PublishProjects,
    [string]$DotNetVersion = (dotnet --version),
    [string]$TargetFramework = "netcoreapp2.1",
	[switch]$ClearNugetCache,
	[switch]$Clean,
    [string]$BootstrapConfiguration = "Release",
    [string]$Assemblyversion = "1.0.0"
)

$DotNetInstallerUri = "https://dot.net/v1/dotnet-install.ps1";
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Make sure tools folder exists
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$ToolPath = Join-Path $PSScriptRoot "tools"
if (!(Test-Path $ToolPath)) {
    Write-Verbose "Creating tools directory..."
    New-Item -Path $ToolPath -Type directory | out-null
}

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

Function Remove-PathVariable([string]$VariableToRemove)
{
  $path = [Environment]::GetEnvironmentVariable("PATH", "User")
  $newItems = $path.Split(';') | Where-Object { $_.ToString() -inotlike $VariableToRemove }
  [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "User")
  $path = [Environment]::GetEnvironmentVariable("PATH", "Process")
  $newItems = $path.Split(';') | Where-Object { $_.ToString() -inotlike $VariableToRemove }
  [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "Process")
}

# Get .NET Core CLI path if installed.
$FoundDotNetCliVersion = $null;
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $FoundDotNetCliVersion = dotnet --version;
}

Write-Host "dotnet version installed: $FoundDotNetCliVersion"

if($FoundDotNetCliVersion -ne $DotNetVersion) {
    $InstallPath = Join-Path $PSScriptRoot ".dotnet"
    if (!(Test-Path $InstallPath)) {
        mkdir -Force $InstallPath | Out-Null;
    }

    (New-Object System.Net.WebClient).DownloadFile($DotNetInstallerUri, "$InstallPath\dotnet-install.ps1");
    & $InstallPath\dotnet-install.ps1 -Version $DotNetVersion -InstallDir $InstallPath -Channel Current;

    Write-Host "dotnet install complete"
    
    $InstallPath = Join-Path $InstallPath dotnet.exe
    Write-Host "Setting alias 'dotnet' to $InstallPath"
    Set-Alias "dotnet" $InstallPath
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
    $env:DOTNET_CLI_TELEMETRY_OPTOUT=1
}

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

# Build the argument list.
$Arguments = @{
    target=$Target;
    configuration=$Configuration;
    verbosity=$Verbosity;
    dryrun=$WhatIf;
    nugetDefaultPushSourceApiKey=$NugetDefaultPushSourceApiKey;
    nugetDefaultPushSourceUrl=$NugetDefaultPushSourceUrl;
    solutionFilePath=$SolutionFilePath;
    assemblyVersion=$Assemblyversion;
    octopusUrl=$OctopusUrl;
    octopusApiKey=$OctopusApiKey;
	octopusProject=$OctopusProject;
	publishProjects=$PublishProjects
}.GetEnumerator() | ForEach-Object { "--{0}=`"{1}`"" -f $_.key, $_.value };

try {
    Push-Location
    Set-Location build
	if($ClearNugetCache) {
		Write-Host "Clearing nuget cache..."
		Invoke-Expression "dotnet nuget locals all -l"
		Invoke-Expression "dotnet nuget locals all -c"
	}

	if($Clean) {
		Write-Host "Cleaning build..."
		Invoke-Expression "rm -r -fo bin\"
		Invoke-Expression "rm -r -fo obj\"
	}
	
    Write-Host "Restoring packages..."
    Invoke-Expression "dotnet restore --configfile $NugetConfigPath"
    if($LASTEXITCODE -eq 0) {
        Write-Output "Compiling build..."
        Invoke-Expression "dotnet publish -c $BootstrapConfiguration /nologo"
        if($LASTEXITCODE -eq 0) {
            Write-Output "Running build..."
            Invoke-Expression "dotnet bin/$BootstrapConfiguration/$TargetFramework/publish/Build.dll $Arguments"
        }
    }
}
finally {
    Pop-Location
    exit $LASTEXITCODE;
}
