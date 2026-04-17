# Supercharging PowerShell with LINQ

Talk given at the PowerShell + DevOps Global Summit on 2026-04-13 by Arnoud Jansveld (Jane Street)

## Demo Instructions

- Run . ./setup.ps1
- Invoke the Pester scripts in the subfolders:
```
Invoke-PesterFormat .\Intersect\Intersect.Tests.ps1 -Detailed -Pause
Invoke-PesterFormat .\Even\Even.Tests.ps1 -Detailed -Pause
Invoke-PesterFormat .\Lookup\Lookup.Tests.ps1 -Pause
Invoke-PesterFormat .\Ast\Ast.Tests.ps1 -Pause
```

NOTE: The Ast demo requires a (large) set of PowerShell modules to be downloaded to C:\Temp\Gallery
