using namespace System.Linq.Expressions

param ([int[]]$Numbers)

# Create parameter expression: int x
$parameter = [Expression]::Parameter([int], 'x')

# Create constant expression: 2
$constant  = [Expression]::Constant(2, [int])

# Create modulo expression: x % 2
$modulo    = [Expression]::Modulo($parameter, $constant)

# Create constant expression: 0
$zero      = [Expression]::Constant(0, [int])

# Create equality expression: (x % 2) == 0
$equals    = [Expression]::Equal($modulo, $zero)

# Compile to delegate: Func<int, bool>
$lambda    = [Expression]::Lambda([Func[int, bool]], $equals, $parameter)
$delegate  = $lambda.Compile()

[System.Linq.Enumerable]::Where($Numbers, $delegate)
