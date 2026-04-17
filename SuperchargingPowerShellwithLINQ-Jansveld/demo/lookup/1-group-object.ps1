param ($InputObject, $Property)

$InputObject | Group-Object $Property -AsHashTable -AsString
