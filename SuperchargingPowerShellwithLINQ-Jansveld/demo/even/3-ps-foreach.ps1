param ([int[]]$Numbers)

foreach ($number in $Numbers) {
    if ($number % 2 -eq 0) {$number}
}
