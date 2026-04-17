using namespace System.Linq
using namespace System.Linq.Expressions
using namespace System.Collections.Generic

<#
.SYNOPSIS
    Groups elements by a property using LINQ ToLookup/ToDictionary with compiled expression trees.
.DESCRIPTION
    Creates an ILookup<string, T> or Dictionary<string, T> from an enumerable, grouped by the
    specified property. Uses expression trees for .NET-speed delegate compilation. Handles both
    typed objects and PSCustomObject/PSObject input.
.EXAMPLE
    Get-Lookup -InputObject $services -Property MachineName
.EXAMPLE
    Get-Lookup -InputObject $services -Property MachineName -AsDictionary
#>
[CmdletBinding()]
param (
    # The collection of objects to group
    [Parameter(Mandatory)]
    [System.Collections.IEnumerable]
    $InputObject,

    # The property name to use as the grouping key
    [Parameter(Mandatory)]
    [string]
    $Property,

    # Return a Dictionary instead of an ILookup (throws on duplicate keys)
    [switch]
    $AsDictionary
)

# Infer the type from the collection type (e.g. [int[]] -> [int])
$type = $InputObject.GetType().GetElementType()
if (-not $type) {
    $type = $InputObject.GetType().GetInterfaces() |
        where {$_.IsGenericType -and $_.GetGenericTypeDefinition() -eq [IEnumerable`1]} |
        foreach {$_.GetGenericArguments()[0]} |
        select -First 1
}

# Does the type have the "real" property?
if ($type -and $type.GetProperty($Property)) {
    $parameterExpression = [Expression]::Parameter($type, 'x')
    $propertyExpression = [Expression]::Property($parameterExpression, $Property)
    $data = $InputObject
}
else {
    # If not, use ETS
    $parameterExpression = [Expression]::Parameter([psobject], 'x')
    $propertiesExpression = [Expression]::Property($parameterExpression, 'Properties')
    $propertyConstant = [Expression]::Constant($Property)
    $itemExpression = [Expression]::Property($propertiesExpression, 'Item', $propertyConstant)
    $propertyExpression = [Expression]::Property($itemExpression, 'Value')
    $data = [Enumerable]::Cast[psobject]($InputObject)
}

$toStringExpression = [Expression]::Call($propertyExpression, 'ToString', [type]::EmptyTypes)
$lambdaExpression = [Expression]::Lambda($toStringExpression, $parameterExpression)
$keySelector = $lambdaExpression.Compile()

if ($AsDictionary) {
    , [Enumerable]::ToDictionary($data, $keySelector)
}
else {
    , [Enumerable]::ToLookup($data, $keySelector)
}
