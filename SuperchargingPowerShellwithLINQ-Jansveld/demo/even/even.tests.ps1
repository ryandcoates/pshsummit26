Describe 'Even filter' {
    BeforeDiscovery {
        $testCases = Get-ChildItem "$PSScriptRoot/[1-9]*.ps1"
    }

    BeforeAll {
        [int[]]$numbers = 1..1000000
    }

    It '<_.BaseName>' -ForEach $testCases {
        $result = & $_.FullName -Numbers $numbers
        $result.Count | Should -Be 500000
        $result[0]    | Should -Be 2
        $result[1]    | Should -Be 4
        $result[-1]   | Should -BeOfType [int]
    }
}
