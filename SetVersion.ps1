function Usage
{
  echo "Usage: ";
  echo "  from cmd.exe: ";
  echo " ";
  echo "  from powershell.exe prompt: ";
  echo " ";
}


function Update-AssemblyInfoSourceVersion
{
  Param ([string]$Version)
  $NewVersion = 'AssemblyVersion("' + $Version + '")';
  $NewFileVersion = 'AssemblyFileVersion("' + $Version + '")';
  foreach ($o in $input) 
  {
    Write-output $o.FullName
    $TmpFile = $o.FullName + ".tmp"

     Get-Content $o.FullName -encoding utf8 |
        %{$_ -replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $NewVersion } |
        %{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $NewFileVersion }  |
        Set-Content $TmpFile -encoding utf8
    
    move-item $TmpFile $o.FullName -force
  }
}

function Update-CsprojSourceVersion
{
  Param ([string]$Version)
  $arr = $Version.Split(".")
  $arr = $arr[0..($arr.Length-2)]
  $threeletterversion = [string]::Join(".",$arr);
  
  $NewProjectVersion = '<Version>' + $threeletterversion;
  $NewProjectAssemblyVersion = '<AssemblyVersion>' + $Version;
  $NewProjectFileVersion = '<FileVersion>' + $Version;
  foreach ($o in $input) 
  {
    Write-output $o.FullName
    $TmpFile = $o.FullName + ".tmp"

     Get-Content $o.FullName -encoding utf8 |
		%{$_ -replace '<Version>[0-9]+(\.([0-9]+|\*)){1,3}', $NewProjectVersion }  |
		%{$_ -replace '<AssemblyVersion>[0-9]+(\.([0-9]+|\*)){1,3}', $NewProjectAssemblyVersion }  |
		%{$_ -replace '<FileVersion>[0-9]+(\.([0-9]+|\*)){1,3}', $NewProjectFileVersion }  |
        Set-Content $TmpFile -encoding utf8
    
    move-item $TmpFile $o.FullName -force
  }
}

function Update-NuspecVersion
{
  Param ([string]$Version)
  $arr = $Version.Split(".")
  $arr = $arr[0..($arr.Length-2)]
  $threeletterversion = [string]::Join(".",$arr);
  
  $NewPackageVersion = '<version>' + $threeletterversion + '</version>';
  $coreDependency = '<dependency id="Struct.PIM.UmbracoCommerce.Connector.Core" version="[' + $threeletterversion + ']" exclude="Build,Analyzers" />';
  
  foreach ($o in $input) 
  {
    Write-output $o.FullName
    $TmpFile = $o.FullName + ".tmp"

     Get-Content $o.FullName -encoding utf8 |
		%{$_ -replace '<version>[0-9]+(\.([0-9]+|\*)){1,3}</version>', $NewPackageVersion }  |
		%{$_ -replace '<dependency id="Struct.PIM.UmbracoCommerce.Connector.Core" version="[[0-9]+(\.([0-9]+|\*)){1,3}]" exclude="Build,Analyzers" />', $coreDependency }  |
        Set-Content $TmpFile -encoding utf8
    
    move-item $TmpFile $o.FullName -force
  }
}

function Update-AllAssemblyInfoFiles ( $version )
{
  foreach ($file in "AssemblyInfo.cs", "AssemblyInfo.vb" ) 
  {
    get-childitem -recurse |? {$_.Name -eq $file} | Update-AssemblyInfoSourceVersion $version ;
  }
}

function Update-CsprojFiles ( $version )
{
  foreach ($file in "Struct.PIM.UmbracoCommerce.Connector.csproj", "Struct.PIM.UmbracoCommerce.Connector.Core.csproj" ) 
  {
    get-childitem -recurse |? {$_.Name -eq $file} | Update-CsprojSourceVersion $version ;
  }
}

function Update-NuspecFiles ( $version )
{
  foreach ($file in "Struct.PIM.UmbracoCommerce.Connector.nuspec", "Struct.PIM.UmbracoCommerce.Connector.Core.nuspec" ) 
  {
    get-childitem -recurse |? {$_.Name -eq $file} | Update-NuspecVersion $version ;
  }
}

# validate arguments 
$r= [System.Text.RegularExpressions.Regex]::Match($args[0], "^[0-9]+(\.[0-9]+){1,3}$");

if ($r.Success)
{
  Update-AllAssemblyInfoFiles $args[0];
  Update-CsprojFiles $args[0];
  Update-NuspecFiles $args[0];
}
else
{
  echo " ";
  echo "Bad Input!"
  echo " ";
  Usage ;
}