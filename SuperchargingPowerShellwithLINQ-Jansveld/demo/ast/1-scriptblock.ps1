using namespace System.Management.Automation.Language
using namespace System.Collections.Generic

param ([IEnumerable[Ast]]$Ast)

$accessControlMethodsPredicate = {
    param (
        [Ast]
        $Ast
    )

    $Ast -is [InvokeMemberExpressionAst] -and -not $Ast.Static -and
        $Ast.Member.ToString() -match '^(Set|Get)AccessControl$'
}

$Ast.FindAll($accessControlMethodsPredicate, $true)
