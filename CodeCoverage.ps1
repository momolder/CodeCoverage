param (
    [string] $rootFolder,
    [string] $configuration = "Debug",
    [string] $dllNameFilter = "*.Test.dll",
    [ValidateSet('Nunit', 'Xunit')]
    [string] $testRunner = "Nunit"
)

function EnsureDir {
    param ([string] $dir)
    try {
        Remove-Item $dir -Force -Recurse
        New-Item -ItemType directory -Path $dir
    }
    catch {}
}

function InstallPackage {
    param ([string] $package)
    nuget install $package -ExcludeVersion -source "https://api.nuget.org/v3/index.json" -force
}

function UpdatePackages {
    Write-Host "Installing nuget packages"
    InstallPackage NUnit.ConsoleRunner
    InstallPackage xunit.runner.console
    InstallPackage OpenCover
    InstallPackage ReportGenerator
}

function CollectTestDlls {
    Write-Host "Collect test dlls"
    $testDlls = Get-ChildItem -Path $rootFolder -Recurse | where-object { $_.fullname -like "*\$configuration\*" -and $_.fullname -notlike "*\obj\*" -and $_.fullname -like $dllNameFilter }
    foreach ($item in $testDlls) {
        $x = $item.fullname
        $testDllArray += "\`"$x\`" " 
        Write-Host $item.fullname
    }
    $testDllArray
}

function RunCoverage {
    param ([string[]] $testDllArray)
    switch ($testRunner) {
        "Nunit" {  
            Write-Host "Run coverage with NUnit runner..."
            & ..\opencover\tools\OpenCover.Console.exe -register:user -target:..\nunit.consoleRunner\tools\nunit3-console.exe -targetargs:$testDllArray -output:opencovertests.xml
        }
        "Xunit" {
            Write-Host "Run coverage with XUnit runner..."
            & ..\opencover\tools\OpenCover.Console.exe -register:user -target:..\xunit.runner.console\tools\net472\xunit.console.exe -targetargs:$testDllArray -output:opencovertests.xml
        }
    }
}

function WriteReport {
    & dotnet ..\reportgenerator\tools\netcoreapp3.0\ReportGenerator.dll -reports:opencovertests.xml -targetdir:..\output
}

function CalculateCoverage {    
    
    $tempDir = ".\temp"
    EnsureDir $tempDir

    $testDllArray = CollectTestDlls

    push-location $tempDir
    RunCoverage $testDllArray
    WriteReport
    pop-location

    Write-Host "Cleanup"

    Remove-Item $tempDir -Force -Recurse
}

function OpenResult {
    & .\output\index.html
}

function main {
    $workDir = $rootFolder + "\CodeCoverage"
    EnsureDir $workDir

    push-location $workDir
    UpdatePackages
    CalculateCoverage
    Pop-Location

    OpenResult
}

main