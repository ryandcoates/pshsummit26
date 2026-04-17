using namespace System.Management.Automation.Language
using namespace System.Linq.Expressions
using namespace System.Collections.Generic

param ([IEnumerable[Ast]]$Ast)

Add-Type -TypeDefinition @'
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

public static class AstPredicate {
    public static System.Func<Ast, bool> IsNonStaticAccessControlInvocation() {
        return ast =>
            ast is InvokeMemberExpressionAst invoke &&
            !invoke.Static &&
            Regex.IsMatch(invoke.Member.ToString(), @"^(Set|Get)AccessControl$");
    }
}
'@

$predicate = [AstPredicate]::IsNonStaticAccessControlInvocation()
$Ast.FindAll($predicate, $true)
