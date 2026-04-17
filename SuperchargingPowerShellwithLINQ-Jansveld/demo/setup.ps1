# Run this dot-sourced: . ./setup.ps1

Add-Type -Path "$PSScriptRoot/progressenumerable.cs"
[ProgressEnumerable]::SoundDirectory = "$PSScriptRoot/sounds/ast"
[ProgressEnumerable]::ReloadSounds()


Write-Progress 'Setting up demo objects'
. "$PSScriptRoot/lookup/service-class.ps1"
. "$PSScriptRoot/ast/ast-modules.ps1"
. "$PSScriptRoot/pester-format.ps1"
