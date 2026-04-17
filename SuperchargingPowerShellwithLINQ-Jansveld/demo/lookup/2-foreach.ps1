param ($InputObject, $Property)

$hash = @{}
foreach ($item in $InputObject) {
    $groupBy = $item.$Property.ToString()
    $list = $hash[$groupBy]
    if ($null -eq $list) {
        $list = [System.Collections.Generic.List[object]]::new()
        $hash[$groupBy] = $list
    }
    $list.Add($item)
}

$hash
