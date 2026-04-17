Describe 'Intersection Methods' {
    BeforeDiscovery {
        $testCases = Get-ChildItem "$PSScriptRoot/[1-9]*.ps1"
    }

    BeforeAll {
        [int[]]$a = 1..10000    | Get-Random -Shuffle
        [int[]]$b = 9900..19900 | Get-Random -Shuffle
    }

    It '<_.BaseName>' -ForEach $testCases {
        $result = & $_.FullName -First $a -Second $b
        $result.Count | Should -Be 101
        $result[0] | Should -BeOfType [int]
    }
}
