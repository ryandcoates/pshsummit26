# The AST demo requires a (large) set of PowerShell modules to be downloaded to 
# C:\Temp\Gallery

using namespace System.Management.Automation.Language
using namespace System.Collections.Generic
using namespace System.Linq

if (-not $allModules) {
    $allModules = [List[Ast]]::new()
    $path = if ($IsLinux) {
        '/mnt/c/temp/Gallery'
    }
    else {
        'C:\Temp\Gallery'
    }
    $ps1 = [System.IO.Directory]::EnumerateFiles($path, '*.psm1', 'AllDirectories')
    $wrapper = [ProgressEnumerable]::Wrap[string]($ps1, 'Generating ')
    foreach ($file in $wrapper) {
        $content = Get-Content $file -Raw
        if (-not [string]::IsNullOrWhiteSpace($content)) {
            $allModules.Add(
                [Parser]::ParseInput(
                    $content,
                    ([ref]$tokens = $null),
                    [ref]$null
                )
            )
        }
    }
}
