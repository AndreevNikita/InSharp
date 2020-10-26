# InSharp


## Example 1
```c#
ILGen<Func<int, int, float>> gen = new ILGen<Func<int, int, float>>("TestFunc1", true);

//Function
gen.Return( Expr.Mul(gen.args[0], gen.Const(3.5f)) ) ;

var func = gen.compile(true); //Our function

Console.WriteLine("Result: {0}", func(15, 3));
```