param (
    $version = '1.0.0.0',
	$Assembly = 'Clinic' 
)

"[assembly:System.Reflection.AssemblyVersion(""$version"")]" | Out-File .\.build\Version.cs -Force
"[assembly:System.Reflection.AssemblyFileVersion(""$version"")]" | Out-File .\.build\Version.cs -Append

"<?define Version = ""$version"" ?>" | Out-File .\.build\Version.wxi -Force
"<Include/>" | Out-File .\.build\Version.wxi -Append


$boosterPath = Get-ChildItem Skytecs.Booster.dll -Recurse;

if( $boosterPath.GetType().BaseType.Name -eq 'Array' ) {
 
 $boosterPath = $boosterPath[-1];

}

$toolsDir = $boosterPath.Directory.FullName;

Import-Module "$toolsDir\Skytecs.Booster.dll"

Open-Model ".\.build\model.xml"

Write-DomainCode > .\KnowledgeDriven.Clinic\Model.cs
Write-DesktopCode > .\KnowledgeDriven.Clinic\Model.UI.cs
Write-Mappings > .\KnowledgeDriven.Clinic\Model.hbm.xml -Concurrency SqlServer
Write-UI > .\KnowledgeDriven.Clinic\Model.xaml


trap [Exception] { 
      write-host
      write-error $("TRAPPED: " + $_.Exception); 
      
      break; 
}