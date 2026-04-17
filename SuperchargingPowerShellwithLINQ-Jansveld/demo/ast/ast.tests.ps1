using namespace System.Management.Automation.Language

Describe 'Ast parsing' {
    BeforeDiscovery {
        $testCases = Get-ChildItem $PSScriptRoot/[1-9]*.ps1
    }

    It '<_.BaseName>' -TestCases $testCases {
        $wrapper = [ProgressEnumerable]::Wrap[Ast](
            $allModules, $_.BaseName.PadRight(30), 100, $true
        )
        $result = & $_.FullName -Ast $wrapper
        # Update this to match the result from the available psm1 samples
        $result.Count | Should -Be 10
        $result[0] | Should -BeOfType [Ast]
     }
}
