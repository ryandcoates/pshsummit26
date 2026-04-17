function Out-Pester {
    [CmdletBinding()]
    param (

        # Parameter help description
        [Parameter(ValueFromPipeline)]
        [System.Management.Automation.InformationRecord]
        $InputObject,

        [switch]
        $Detailed,

        [switch]
        $Pause
    )

    begin {
        $showOutput = $false
        Write-Host ''
    }

    process {
        if ($InputObject -match 'Running tests from') {
            $showOutput = $true
            return
        }
        if (-not $showOutput) {
            return
        }

        if ($InputObject -match '\((([\d\.]+)(m?)s)\|') {
            if ($Detailed) {$PSCmdlet.WriteInformation($InputObject)}
            if ($Pause) {[void][console]::ReadKey($true)}
        }
        elseif ($InputObject -match '\[\+\]') {
            if ($Detailed) {$PSCmdlet.WriteInformation($InputObject)}
        }
        else {
            $PSCmdlet.WriteInformation($InputObject)
        }
    }
}

function Invoke-PesterFormat {
    [CmdletBinding()]
    param (

        # Path
        [Parameter(ValueFromPipeline)]
        [string]
        $Path,

        [switch]
        $Detailed,

        [switch]
        $Pause
    )

    process {
        Invoke-Pester -Path $Path -Output Detailed 6>&1 | Out-Pester -Detailed:$Detailed -Pause:$Pause
    }
}
