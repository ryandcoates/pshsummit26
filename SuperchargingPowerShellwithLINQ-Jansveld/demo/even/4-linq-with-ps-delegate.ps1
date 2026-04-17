param ([int[]]$Numbers)

[Func[int, bool]]$filter = {param ([int]$number) $number % 2 -eq 0}

[System.Linq.Enumerable]::Where($Numbers, $filter)
