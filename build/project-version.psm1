<# 
 .Synopsis
  Updates project.json version

 .Description
  Updates the project.json version to include the build number as pre-release suffix.
  If the current build is a tag, the the build number is *NOT* appended.

 .Parameter Path
  The path to the project.json file.

 .Parameter BuildNumber
  The build number for the pre-release suffix.

 .Parameter IsPreRelease
  Flag that indicates whether the current build is a pre-release.

 .Parameter PreReleasePrefix
  The prefix that is used for the pre-release suffix in combination with the build number.

 .Returns
  The determined build number.

 .Example
   > Set-ProjectVersion -path "src\MyApp\project.json"
   > 1.0.0

 .Example
   > Set-ProjectVersion -path "src\MyApp\project.json" -buildNumber 321 -isPreRelease $true
   > 1.0.0-ci321

 .Example
   > Set-ProjectVersion -path "src\MyApp\project.json" -buildNumber 321 -isPreRelease $true -preReleasePrefix "dev"
   > 1.0.0-dev321
#>
function Set-ProjectVersion {
param(
	[string] $path,
	[int] $buildNumber = 0,
	[bool] $isPreRelease = $false,
	[string] $preReleasePrefix = "ci"
)
	$str = Get-Content -Raw -Path $path
	$json = ConvertFrom-Json $str

	$version = $buildVersion = $json.version

	if ($isPreRelease -eq $true) {
		$buildVersion = "$version-$preReleasePrefix$buildNumber"
		$str.Replace("""version"": ""$version"",", """version"": ""$buildVersion"",") | Set-Content $path
	}

	return $buildVersion
}

Export-ModuleMember -Function Set-ProjectVersion