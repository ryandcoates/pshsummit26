param ([int[]]$Numbers)

$Numbers.Where({$_ % 2 -eq 0})
