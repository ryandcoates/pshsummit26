param ([int[]]$Numbers)

$Numbers | Where-Object {$_ % 2 -eq 0}
