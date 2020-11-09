# InSharp
Library for runtime MSIL functions compilation

## How to use

0. Create ILGen object `ILGen<[Method delegate type]> gen = new ILGen<[Method delegate type]>([Mathod name], [enable private fields and classes access]);`
0. Write code with statements
	* `gen.Line` for statements
	    * Use class Expr to use operators (Example: `Expr.Mul(gen.args[0], gen.Const(3.5f))`, `Expr.Greater(gen.args[0], 0)`)
	    * To set value use *Set* method (Example: `gen.Line(i.Set(0));`) **!Set is the statement and must be in gen.Line**
	* `gen.Return` to return value
	* `gen.If` / `gen.ElseIf` / `gen.Else` / `gen.EndIf` for if construction
	* `gen.While` / `gen.EndWhile` for while cicles

0. Call `gen.compile([enable debug info]);` to compile your function

## Simple shell get/set/call/construct functions
Maybe you need only to compile function-caller / constructor / setter / getter for unknown type?
You can use ILTemplate for these tasks:
#### Memebr getters
* `ILTemplate.CommonMemberGetter` - for static and instance class members get. Returns *Func<object, object>*, where the first argument - object instance (if instance member) and the return value - field value
* `ILTemplate.StaticMemberGetter` - for stastic class members get. As CommonMember getter, but the return function doesn't receive the object instance argument
* `ILTemplate.InstanceMemberGetter` - for instance class members get. The first argument of the return function - object instance, return value - field value

#### Member setters
* `CommonMemberSetter` - for static and instance class members set. Returns *Action<object, object>* where first argument - object instance (for instance fields) and the second - set value
* `StaticMemberSetter` - for static members set. Result Action< object > is without object instance arg 
* `InstanceMemberSetter` for instance members set. Returns *Action<object, object>* where first argument - object instance and second - set value

#### Class constructor
* Use `CommonObjectCreator` to compile function *Func<object[], object>*, that calls constructor with args in array of object and returns a new object instance

#### Method call shell
* `CommonCallShell` - call shell for static and instance class methods, with and without return type. Returns *Func<object, object[], object>* , where the first argument - class instance (if calls instance function), the second - call function args array. Returns null, if call method has no return type
* `CommonStaticCallShell` - call shell for static class methods with and without return type. Returns *Func<object[], object>*, where the first argument - call function args array.
* `StaticActionCallShell` - call shell for static class methods without return type. Returns *Action<object[]>*, where the first argument - call function args array. 
* `StaticFunctionCallShell` - call shell for static class methods without return type. Returns *Func<object[], object>*, where the first argument - call function args array.
* `CommonInstanceCallShell` - call shell for instance class methods with and without return type. Returns *Func<object, object[], object>* where the first arg - object instance, the second - function call args array. Returns null, if the call method has no return type
* `InstanceActionCallShell` - call shell for instance class methods without return type. Returns *Action<object, object[]>* where the first arg - object instance, the second - function call args array.
* `InstanceFunctionCallShell` - call shell for instance class methods with return type. Returns *Func<object, object[], object>* where the first arg - object instance, the second - function call args array.

## Example 1
Function returns (first arg) * 3.5
```c#
ILGen<Func<int, float>> gen = new ILGen<Func<int, float>>("Example1_func", true);

//Function
gen.Return( Expr.Mul(gen.args[0], gen.Const(3.5f)) ) ;

var func = gen.compile(true); //Our function

Console.WriteLine("Result: {0}", func(15));
```

## Example 2
If/elseif/else constructions
```c#
var gen = new ILGen<Action<int>>("TestFunc5_1", true);

//Function
gen.If(Expr.Greater(gen.args[0], 0));
//If arg0 > 0
gen.Line(Expr.CallStatic(typeof(Console), "WriteLine", "{0} is positive", Expr.CreateArray(typeof(object), gen.args[0])));
gen.ElseIf(Expr.Less(gen.args[0], 0));
//Else if arg0 < 0
gen.Line(Expr.CallStatic(typeof(Console), "WriteLine", "{0} is negative", Expr.CreateArray(typeof(object), gen.args[0])));
gen.Else();
//else (arg0 == 0)
gen.Line(Expr.CallStatic(typeof(Console), "WriteLine", "Zero"));
gen.EndIf();


var func = gen.compile(true);
```

## Example 3
Fibonacci numbers array gen
```c#
var gen = new ILGen<Func<int, long[]>>("TestFunc6", true);

//Function
ILVar resultArray = gen.DeclareVar(typeof(long[]));
ILVar arrayIndex = gen.DeclareVar(typeof(int));
gen.Line(resultArray.Set(Expr.InitArray(typeof(long), gen.args[0])));
gen.Line(resultArray.Index(0).Set(0));
gen.Line(resultArray.Index(1).Set(1));
gen.Line(arrayIndex.Set(2));
gen.While(Expr.NotEquals(arrayIndex, resultArray.ArrayLength));
	gen.Line(resultArray.Index(arrayIndex).Set(resultArray.Index(arrayIndex - 1) + resultArray.Index(arrayIndex - 2)));
	gen.Line(arrayIndex.Set(arrayIndex + 1));
gen.EndWhile();
gen.Return(resultArray);

var func = gen.compile(true);
```

## Example 4
Double fields of unknown type sum function
```c#
var gen = new ILGen<Func<T, double>>(typeof(T).Name + "_fields_sum", true);

ILVar counter = gen.DeclareVar<double>();
gen.Line(counter.Set(0.0));
foreach(FieldInfo fieldInfo in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(info => info.FieldType == typeof(double)))
	gen.Line(counter.Set(counter + gen.args[0].Field(fieldInfo)));
gen.Return(counter);

var func = gen.compile(true);
```


## Example 5
Create a matrix and output it to console

```c#
var gen = new ILGen<Action>("TestFunc14", true);

ILVar matrix = gen.DeclareVar(typeof(int[,]));
gen.Line(matrix.Set(Expr.CreateArray(typeof(int), new Expr[] { 
		1, 2, 3,
		4, 5, 6,
		7, 8, 9
	}, new int[] { 3, 3})));

ILVar index1 = gen.DeclareVar(typeof(int));
gen.Line( index1.Set(0) );
ILVar index2 = gen.DeclareVar(typeof(int));
gen.Line( index2.Set(0) );
ILVar lineString = gen.DeclareVar(typeof(string));

gen.While( Expr.Less(index1, matrix.GetArrayDimensionLength(0)) );
	gen.Line( lineString.Set("") );
	gen.Line( index2.Set(0) );
	gen.While( Expr.Less(index2, matrix.GetArrayDimensionLength(1)) );
		gen.Line( lineString.Set(Expr.Add(Expr.Add(lineString, matrix.Index(index1, index2).CompatiblePass(typeof(string))), "; ")) );
		gen.Line( index2.Set(Expr.Add(index2, 1)) );
	gen.EndWhile();
	
	gen.Line(Expr.CallStatic(typeof(Console), "WriteLine", lineString));

	gen.Line( index1.Set(Expr.Add(index1, 1)) );
gen.EndWhile();

var func =  gen.compile(true);
```