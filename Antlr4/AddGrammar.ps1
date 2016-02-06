
function Add-Dependency {
    Param($group, $suffix)
    $el = $doc.CreateElement('Compile', $xmlns)
    $include = $doc.CreateAttribute('Include')
    $include.Value = "$($grammarname)$($suffix).cs"
    $el.Attributes.Append($include)
    $dep = $doc.CreateElement('DependentUpon', $xmlns)
    $dep.AppendChild($doc.CreateTextNode($grammarfile))
    $el.AppendChild($dep)
    $group.AppendChild($el)
} 

[xml]$doc = New-Object XML
$grammarname = $args[0]
[string]$projfile = "$($args[1])"

$toolpath = "`$(SolutionDir)ANTLR4\antlr-4.5.1-complete.jar"
$xmlns = "http://schemas.microsoft.com/developer/msbuild/2003"
$grammarfile = "$($grammarname).g4"

$doc.Load($projfile)
$xmlnsm = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)

$proj = $doc.GetElementsBytagName('Project') | Select-Object -first 1
$existingnode = $proj.SelectSingleNode("ItemGroup/None[@Include='$($grammarfile)']", $xmlnsm)
Write-Host $existingnode

if($existingnode -eq $null)
{

    $group = $doc.CreateElement('ItemGroup', $xmlns)

    $grammar = $doc.CreateElement('None', $xmlns)
    $include = $doc.CreateAttribute('Include')
    $include.Value = $grammarfile

    $grammar.Attributes.Append($include)

    $group.AppendChild($grammar)
    Add-Dependency $group 'BaseListener'
    Add-Dependency $group 'BaseVisitor'
    Add-Dependency $group 'Lexer'
    Add-Dependency $group 'Parser'
    Add-Dependency $group 'Visitor'
    Add-Dependency $group 'Listener'

    $propgroup = $doc.CreateElement('PropertyGroup', $xmlns)
    $prebuild = $doc.CreateElement('PreBuildEvent', $xmlns)
    $prebuild.AppendChild($doc.CreateTextNode("java -cp ""$($toolpath)"" org.antlr.v4.Tool -Dlanguage=CSharp -visitor ""`$(ProjectDir)$($grammarname).g4"""))
    $propgroup.AppendChild($prebuild)

    $proj.AppendChild($group)
    $proj.AppendChild($propgroup)


    $doc.Save($projfile)

} else {
    Write-Host "Grammar already exists"
}

