using namespace System.Management.Automation.Language
using namespace System.Linq.Expressions
using namespace System.Collections.Generic

param ([IEnumerable[Ast]]$Ast)

$param          = [Expression]::Parameter([Ast], 'ast')
$asInvokeMember = [Expression]::Convert($param, [InvokeMemberExpressionAst])
$matchMethod    = [regex].GetMethod('IsMatch', [type[]]@([string], [string]))
$isInvokeMember = [Expression]::TypeIs($param, [InvokeMemberExpressionAst])
$memberProperty = [Expression]::Property($asInvokeMember, 'Member')
$memberToString = [Expression]::Call($memberProperty, 'ToString', $null)
$regexConstant  = [Expression]::Constant('^(Set|Get)AccessControl$')
$matchesPattern = [Expression]::Call($matchMethod, $memberToString, $regexConstant)
$isNotStatic    = [Expression]::Not([Expression]::Property($asInvokeMember, 'Static'))
$andExpression  = [Expression]::AndAlso($isNotStatic, $matchesPattern)
$predicate      = [Expression]::AndAlso($isInvokeMember, $andExpression)
$delegate       = [Expression]::Lambda([Func[Ast, bool]], $predicate, $param).Compile()

$Ast.FindAll($delegate, $true)
