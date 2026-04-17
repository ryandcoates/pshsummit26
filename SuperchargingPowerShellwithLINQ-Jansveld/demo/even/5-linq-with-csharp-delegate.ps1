param ([int[]]$Numbers)

Add-Type -TypeDefinition @'
using System;
public class NumberFilter
{
    public static readonly Func<int, bool> IsEven = n => n % 2 == 0;
}
'@

$delegate = [NumberFilter]::IsEven
[System.Linq.Enumerable]::Where($Numbers, $delegate)
