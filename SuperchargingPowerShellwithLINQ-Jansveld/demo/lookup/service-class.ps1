enum ServiceStartMode {
    Boot
    System
    Automatic
    Manual
    Disabled
}

enum ServiceStatus {
    Stopped
    StartPending
    StopPending
    Running
    ContinuePending
    PausePending
    Paused
}

class Service {
    [string] $Name
    [ServiceStartMode] $StartType
    [bool] $DelayedAutoStart
    [string] $MachineName
    [ServiceStatus] $Status

    static [Service] Random([int]$Machine, [int]$Service) {
        return [Service]@{
            Name             = "Service$Service"
            StartType        = [ServiceStartMode](Get-Random -Maximum 5)
            DelayedAutoStart = Get-Random -Maximum 1
            MachineName      = "Host$Machine"
            Status           = [ServiceStatus](Get-Random -Maximum 7)
        }
    }
}

if (-not $script:allServices) {
    [Service[]]$script:allServices = foreach ($i in 1..5000) {foreach ($j in 1..200) {[Service]::Random($i, $j)}}
}
