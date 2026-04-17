Describe 'Lookup functions' {
    BeforeDiscovery {
        $testCases = Get-ChildItem "$PSScriptRoot/[1-9]*.ps1"
    }

    It '<_.BaseName>' -TestCases $testCases {
        $typedData = [ProgressEnumerable]::Wrap[Service](
            $allServices, $_.BaseName.PadRight(30)
        )
        $result = & $_.FullName $typedData 'Status'
        $result.Count | Should -Be 7
        $result['Stopped'][0] | Should -BeOfType 'Service'
    }
}
