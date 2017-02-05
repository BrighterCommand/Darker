<# 
 .Synopsis
  Updates project version
 .Description
  Updates the csproj version to include the build number as pre-release suffix.
  If the current build is a tag, the the build number is *NOT* appended.
 .Parameter Path
  The path to the project file.
 .Parameter SetAppveyorVersion  
  Flag that indicates whether the script should also set the AppVeyor version number.
 .Returns
  The determined build number.
 .Example
   > Set-Version -path "src\MyApp\MyApp.csproj"
   > 1.0.0
 .Example
   > Set-Version -path "src\MyApp\MyApp.csproj" -setAppveyorVersion
   > 1.0.0-ci321
#>
function Set-Version {
param(
	[string] $path,
	[switch] $setAppveyorVersion = $false
)
	$versionTag = "VersionPrefix";

	$csproj = Get-Content -Raw -Path $path
	$version = $buildVersion = [regex]::match($csproj, "<$versionTag>(.*)</$versionTag>").Groups[1].Value

	if ($env:APPVEYOR_REPO_TAG -eq $false) {
		$buildVersion = "$version-ci$($env:APPVEYOR_BUILD_NUMBER)"
		$csproj.Replace("<$versionTag>$version</$versionTag>", "<$versionTag>$buildVersion</$versionTag>") | Set-Content $path
	}
	
	if ($setAppveyorVersion -eq $true) {
		Update-AppveyorBuild -Version $buildVersion
	}

	return $buildVersion
}

Export-ModuleMember -Function Set-Version