param (
    [string]$build,
	[string]$sqlPackagePath = "C:\Program Files (x86)\Microsoft SQL Server\110\DAC\bin\sqlpackage.exe",
	[string]$ilmerge = ".\Tools\ILMerge.Tools.2.14.1208\ilmerge.exe",
	[string]$crcli = ".\Tools\ConfuserEx.0.6.0.0\confuser.cli.exe",
	[string]$tempDeployPath = ".build\temp\"

)


trap [Exception] { 
      write-host
      write-error $("TRAPPED: " + $_.Exception) 
      
      exit 1
}

$webDeployV3 = "$((Get-Item "HKLM:\SOFTWARE\Microsoft\IIS Extensions\MSDeploy\3").GetValue("InstallPath"))msdeploy.exe";


New-Item -ItemType Directory  $tempDeployPath -Force


function Deploy-Website{

	param (
		[string]$server, 
		[string]$setParam, 
		[string]$deployPath
	)

	Write-Host "Deploying the website" 

	$errorFile = [IO.Path]::GetTempFileName()
	$outFile = [IO.Path]::GetTempFileName()

    $deployPath = (Get-Item $deployPath).FullName

	$deployArgs = "-verb:sync -source:package=""$deployPath"" -dest:auto,computerName=$server,includeAcls=""False"" -setParamFile:""$setParam""";

	$deploy = (Start-Process $webDeployV3 -ArgumentList $deployArgs -Wait -NoNewWindow -Passthru -RedirectStandardError $errorFile -RedirectStandardOutput $outFile)

	$errorMessage = $(Get-Content $errorFile -Encoding oem)

	$message = $(Get-Content $outFile -Encoding oem)

	Remove-Item $errorFile;
	Remove-Item $outFile;

	$message | Write-Output

	if(($deploy.ExitCode -ne 0) -or ($errorMessage.Length -gt 0))
	{
		Write-Host 
		"Failed to deploy  site to server '$server'. Error message is: " | Write-Output
		Write-Output $errorMessage
		exit 1;
	}

}

New-Alias Publish-SqlPackage $sqlPackagePath

function Deploy-Database{
	param(
		[string] $dacpac,
		[string] $profile
	)

	Publish-SqlPackage /a:'publish' /sf:$dacpac /pr:$profile
}


function ILMerge{
	param(
		[string]$log = ".\.build\lmerge-log.txt",
		[string]$excludeList = ".\.build\ilmerge-excludelist.txt",
		[string]$output,
		[string[]]$inputs
	)

    Get-Item $log | Remove-Item 
    

	$quotedInput = ""

    ($inputs | foreach { $quotedInput += """$_"" " } );

    

	$argsList = "/ndebug /targetplatform:v4 /log:""$log"" /internalize:$excludeList /out:""$output"" $quotedInput" 
	
    "Running ilmerge:" | Out-Host
    "$ilmerge $argsList" | Out-Host

	Start-Process $ilmerge -ArgumentList $argsList -Wait
}


function Obfuscate{
	param(
		[string]$projectPath,
		[string]$binPath
	)

    $crPath = $projectPath

    $crproj = [xml](Get-Content $crPath)

    $crproj.DocumentElement.SetAttribute("baseDir",$binPath);
    $crproj.DocumentElement.SetAttribute("outputDir","$binPath\protected");

    $crproj.Save((Get-Item $projectPath).FullName);

	
    "Running ConfuserEx cli:" | Out-Host
    "$crcli ""$crPath""" | Out-Host


    Start-Process $crcli -ArgumentList "-n $crPath" -Wait 
}

function Compress-Zip{
    param($source, $dest)

    Start-Process ".\Tools\7z.15.14\7za.exe" -ArgumentList "a $dest -y $source" -Wait
}

function Expand-Zip{
    param($source, $dest)

    Start-Process ".\Tools\7z.15.14\7za.exe" -ArgumentList "x $source -y -o$dest" -Wait
}

function Repack{

    param([string]$packagePath)

    $tempDeployPath = (Get-Item $tempDeployPath).FullName
    $packagePath = (Get-Item $packagePath).FullName



    New-Item -ItemType Directory "$tempDeployPath\clinic"

    Expand-Zip -source $packagePath -dest "$tempDeployPath\clinic"

    $binPath = (Get-ChildItem -Path "$tempDeployPath\clinic\" -Filter "Skytecs.Clinic.dll" -Recurse).DirectoryName

    $inputs = @("$binPath\Skytecs.Clinic.WebUI.dll", "$binPath\Skytecs.Clinic.dll")

    ILMerge -output "$binPath\Skytecs.Clinic.WebUI.Merged.dll" -inputs $inputs

    Obfuscate -projectPath ".\.build\confuserex.crproj" -binPath $binPath

    $inputs += "$binPath\Skytecs.Clinic.WebUI.Merged.dll"


    $inputs | foreach { Remove-Item -LiteralPath $_ -Force }

    Copy-Item "$binPath\protected\Skytecs.Clinic.WebUI.Merged.dll" "$binPath\Skytecs.Clinic.WebUI.Merged.dll" -Force

    Remove-Item "$binPath\protected" -Recurse

    Remove-Item "$((Get-Item $packagePath).DirectoryName)\clinic_unprotected.zip"

    Rename-Item $packagePath -NewName "clinic_unprotected.zip" -Force

    Compress-Zip -source "$tempDeployPath\clinic\*" -dest $packagePath 

    Remove-Item "$tempDeployPath\clinic\" -Recurse
}
    


if ($build -eq "Stage")
{
    #Repack -packagePath ".\.publish\clinic\Skytecs.Clinic.WebUI.zip"
	#Repack -packagePath ".\.publish\mypatients\Skytecs.MyPatients.zip"

	"Publishing staging database..." | Write-Output
	Deploy-Database -dacpac '.\bin\Release\skytecs.clinic.database.dacpac' -profile '.\bin\Release\clinic.stage.publish.xml'

	"Publishing site Clinic.WebUI" | Write-Output
	Deploy-Website -server "web01.skytecs.local" -setParam ".build\stage.SetParameters.xml" -deployPath ".\.publish\clinic\Skytecs.Clinic.WebUI.zip";

	"Publishing site Clinic.WebUI for functional tests" | Write-Output
	Deploy-Website -server "web01.skytecs.local" -setParam ".build\tests.SetParameters.xml" -deployPath ".\.publish\clinic\Skytecs.Clinic.WebUI.zip";

	"Publishing site MyPatients" | Write-Output
	Deploy-Website -server "web01.skytecs.local" -setParam ".build\Landings.SetParameters.xml" -deployPath ".\.publish\mypatients\Skytecs.MyPatients.zip";
}

