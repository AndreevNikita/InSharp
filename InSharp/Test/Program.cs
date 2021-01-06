using InSharp;
using LowLevelOpsHelper;
using System;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using Test;

namespace InSharpTester {

	public class Vector { 
		public double x, y;
		double _z;
		public double z { 
			get { 
				return _z;
			} 
			set { 
				_z = value;
			}
		}

		public double Length { 
			get => Math.Sqrt(x * x + y * y + z * z);
		} 

		public Vector(double x, double y, double z) { 
			this.x = x;
			this.y = y;
			this._z = z;
		}

		public static Vector operator+(Vector a, Vector b) { 
			return new Vector(a.x + b.x, a.y + b.y, a.z + b.z);
		}

		public static Vector operator*(Vector a, double b) { 
			return new Vector(a.x * b, a.y * b, a.z * b);
		}

		public override string ToString() { 
			return string.Format("({0}; {1}; {2})", x, y, z);
		}
	}

	public class MyCustomAttribute : Attribute { 
	}

	public static class VectorExtend { 
		public static bool IsNull(this Vector vec) { 
			return object.ReferenceEquals(vec, null);
		}
	}

	public struct StructVector {
		public double x, y;

		public StructVector(double x, double y) {
			this.x = x;
			this.y = y;
		}

		public override string ToString() {
			return $"{x}; {y}";
		}
	}

	public class Program {

		public static void Main(string[] args) { 


			testFunc1();
			testFunc2();
			testFunc3();
			testFunc4();
			testFunc5_1();
			testFunc5_2();
			testFunc5_3();
			testFunc6();
			testFunc7();
			testFunc8();
			testFunc9();
			testFunc10();
			testFunc11();
			testFunc12();
			testFunc13();
			testFunc14();
			testFunc15();
			testFunc16();
			testFunc17();
			testFunc18();
			testFunc19();
			testFunc20();
			testFunc21();
			testFunc22();
			testFunc23();
			testFunc24();
			//TestTemplate.Test();
			Console.ReadKey();
		}

		//Simple multiplication
		static void testFunc1() { 
			ILGen<Func<int, int, float>> gen = new ILGen<Func<int, int, float>>("TestFunc1", true);

			//Function
			gen.Return( Expr.Mul(gen.args[0], gen.Const(3.5f)) ) ; //Compile function

			var func = gen.compile(true);
			Console.WriteLine("Result: {0}", func(15, 3));
		}

		//Add vector object and vector object
		static void testFunc2() {

			var gen = new ILGen<Func<Vector, Vector, Vector>>("TestFunc2", true);

			//Function
			gen.Return( Expr.Add(gen.args[0], gen.args[1]) ) ;


			var func = gen.compile(true);
			Console.WriteLine("Result: {0}", func(new Vector(12, 5, 6), new Vector(8, 2, 2)));
		}

		//Multiply Vector object and int
		static void testFunc3() {
			var gen = new ILGen<Func<Vector, Vector>>("TestFunc3", true);

			//Function
			gen.Return( Expr.Mul(gen.args[0], 5) ) ;


			var func = gen.compile(true);
			Console.WriteLine("Result: {0}", func(new Vector(12, 5, 6)));
		}

		static void func4Orig(Vector arg0) {
			object[] args = new object[] {
				arg0.x,
				arg0.y,
				arg0.z,
			};
			Console.WriteLine("Vectors z coords: {0}, {1}, {2}", args);
			arg0.z = arg0.Length;
		}

		//Print property and set it to Length of vector
		static void testFunc4() { 
			var gen = new ILGen<Action<Vector>>("TestFunc4", true);

			//Function
			gen.Line(Expr.CallStatic(typeof(Console), "WriteLine", Expr.Const("Vectors x, y, z coords: {0}; {1}; {2}"), Expr.CreateArray(typeof(object), gen.args[0].Member("x"), gen.args[0].Member("y"), gen.args[0].Member("z"))));
			gen.Line(gen.args[0].Member("z").Set(gen.args[0].Member("Length")));
			//gen.Return( gen.Op(gen.args[0], Ops.Mul, 5) ) ;


			var func = gen.compile(true);
			Vector vec = new Vector(3.0, 0.0, 4.0);
			func(vec);
			Console.WriteLine("Changed vector: {0}", vec);
		}

		//Test if construction
		static void testFunc5_1() {
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
			func(15);
			func(3);
			func(0);
			func(-90);
		}

		//Overloaded operators
		static void testFunc5_2() {
			var gen = new ILGen<Action<int>>("TestFunc5_2", true);

			//Function
			gen.If(gen.args[0] > 0);
			//If arg0 > 0
				gen.Line(Expr.CallStatic(typeof(Console), "WriteLine", "{0} is positive", Expr.CreateArray(typeof(object), gen.args[0])));
			gen.ElseIf(gen.args[0] < 0);
			//Else if arg0 < 0
				gen.Line(Expr.CallStatic(typeof(Console), "WriteLine", "{0} is negative", Expr.CreateArray(typeof(object), gen.args[0])));
			gen.Else();
			//else (arg0 == 0)
				gen.Line(Expr.CallStatic(typeof(Console), "WriteLine", "Zero"));
			gen.EndIf();


			var func = gen.compile(true);
			func(15);
			func(3);
			func(0);
			func(-90);
		}

		//Overloaded operators
		static void testFunc5_3() {
			var gen = new ILGen<Action<int>>("TestFunc5_3", true);

			//Function
			gen.If(Expr.Equals(gen.args[0], 0));
				gen.Line(Expr.CallStatic(typeof(Console), "WriteLine", "Zero"));
			gen.EndIf();


			var func = gen.compile(true);
			func(15);
			func(3);
			func(0);
			func(-90);
		}

		//Fibonacci + test while construcion
		static void testFunc6() {
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
			long[] functionResultArray = func(100);
			for(int index = 0; index < functionResultArray.Length; index++)
				Console.Write("{0}; ", functionResultArray[index]);
			Console.WriteLine();
			
		}

		//Identity 2D matrix
		static void testFunc7() { 
			var gen = new ILGen<Func<int, double[,]>>("TestFunc7", true);

			ILVar resultArray = gen.DeclareVar(typeof(double[,]));
			ILVar i = gen.DeclareVar(typeof(int));
			ILVar j = gen.DeclareVar(typeof(int));
			gen.Line(resultArray.Set(Expr.InitArray(typeof(double), gen.args[0], gen.args[0])));
			gen.Line(i.Set(0));
			gen.While(i < gen.args[0]);
				gen.Line(j.Set(0));
				gen.While(j < gen.args[0]);
					gen.If(Expr.Equals(i, j));
						gen.Line(resultArray.Index(i, j).Set(1.0));
					gen.Else();
						gen.Line(resultArray.Index(i, j).Set(0.0));
					gen.EndIf();
					gen.Line(j.Set(j + 1));
				gen.EndWhile();
				gen.Line(i.Set(i + 1));
			gen.EndWhile();
			gen.Return(resultArray);
			var func = gen.compile(true);
			
			double[,] result = func(20);
			
			for(int arrayI = 0; arrayI < result.GetLength(0); arrayI++) {
				for(int arrayJ = 0; arrayJ < result.GetLength(0); arrayJ++)
					Console.Write("{0}; ", result[arrayI, arrayJ]);
				Console.WriteLine();
			}
		}

		//Vectors array sum
		static void testFunc8() { 
			var gen = new ILGen<Func<Vector[], Vector>>("TestFunc8", true);

			ILVar resultVector = gen.DeclareVar<Vector>();
			ILVar currentIndex = gen.DeclareVar<int>();

			gen.Line(resultVector.Set(Expr.NewObject(typeof(Vector), 0.0, 0.0, 0.0)));
			gen.Line(currentIndex.Set(0));
			gen.While(currentIndex < gen.args[0].ArrayLength);
				gen.Line(resultVector.Set(resultVector + gen.args[0].Index(currentIndex)));
				gen.Line(currentIndex.Set(currentIndex + 1));
			gen.EndWhile();

			gen.Line(Expr.CallStatic(typeof(Console), "WriteLine", "Result vector from function: {0}", resultVector));
			gen.Return(resultVector);

			var func = gen.compile(true);
			Vector result = func(new [] { new Vector(2, 4.5, 1), new Vector(17, 23, 50.3), new Vector(12.5, 22, 7) });
			Console.WriteLine("Result from parent function: {0}", result);
		} 

		//Num fields sum
		static Func<T, double> compile9<T>() { 
			var gen = new ILGen<Func<T, double>>(typeof(T).Name + "_fields_sum", true);

			ILVar counter = gen.DeclareVar<double>();
			gen.Line(counter.Set(0.0));
			foreach(FieldInfo fieldInfo in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(info => info.FieldType == typeof(double)))
				gen.Line(counter.Set(counter + gen.args[0].Field(fieldInfo)));
			gen.Return(counter);

			return gen.compile(true);
		}

		static void testFunc9() { 
			
			var func = compile9<Vector>();
			Random rand = new Random();
			for(int counter = 0; counter < 20; counter++) { 
				Vector checkVector = new Vector(Math.Round((rand.NextDouble() * 40.0 - 20.0), 1), Math.Round((rand.NextDouble() * 40.0 - 20.0), 1), Math.Round((rand.NextDouble() * 40.0 - 20.0), 1));
				Console.WriteLine("Sum result for {0}: {1}", checkVector, func(checkVector));
			}
			
		} 


		//Zero all function
		static Action<T> compile10<T>() { 
			var gen = new ILGen<Action<T>>(typeof(T).Name + "_fields_sum", true);

			foreach(FieldInfo fieldInfo in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(info => info.FieldType == typeof(double)))
				gen.Line(gen.args[0].Field(fieldInfo).Set(0.0));
			gen.Return();

			return gen.compile(true);
		}

		static void testFunc10() { 
			
			var func = compile10<Vector>();
			Random rand = new Random();
			for(int counter = 0; counter < 20; counter++) { 
				Vector checkVector = new Vector(Math.Round((rand.NextDouble() * 40.0 - 20.0), 1), Math.Round((rand.NextDouble() * 40.0 - 20.0), 1), Math.Round((rand.NextDouble() * 40.0 - 20.0), 1));
				Console.WriteLine("Before: {0}", checkVector);
				func(checkVector);
				Console.WriteLine("After: {0}", checkVector);
			}
			
		} 

		//Structs and classes methods, fields and 
		class AClass { 
			int foo;
			int Bar { get; set; }

			void baz() { 
				Console.WriteLine("AClass: foo: {0}; Bar: {1}", foo, Bar);
			}

			AClass GetMe() { 
				return this;
			}

			AClass GetMe(AClass arg1, AClass arg2) { 
				return this;
			}
		}

		struct AStruct { 
			int foo;
			int Bar { get; set; }

			void baz() { 
				Console.WriteLine("AStruct: foo: {0}; Bar: {1}", foo, Bar);
			}

			AStruct GetMe() { 
				return this;
			}

			AStruct GetMe(AStruct arg1, AStruct arg2) { 
				return this;
			}

		}

		//Fields and properties test
		static void testFunc11() { 
			var gen = new ILGen<Action>("TestFunc11", true);

			ILVar var_aclass = gen.DeclareVar<AClass>();
			ILVar var_astruct = gen.DeclareVar<AStruct>();
			ILVar var_buffer = gen.DeclareVar<int>();
			gen.Line( Expr.CallStatic(typeof(Console), "WriteLine", "Init objects...") );
			//Init AClass object
			gen.Line( var_aclass.Set(Expr.NewObject(typeof(AClass))) );
			gen.Line( var_aclass.Member("foo").Set(11) );
			gen.Line( var_aclass.Member("Bar").Set(12) );
			
			//Init AStruct object
			gen.Line( var_astruct.Set(Expr.NewObject(typeof(AStruct))) );
			gen.Line( var_astruct.Member("foo").Set(21) );
			gen.Line( var_astruct.Member("Bar").Set(22) );

			gen.Line( var_aclass.CallMethod("baz") );
			gen.Line( var_astruct.CallMethod("baz") );

			//Exchange check
			gen.Line( Expr.CallStatic(typeof(Console), "WriteLine", "Change fields...") );
			gen.Line( var_buffer.Set(var_aclass.Member("foo")) );
			gen.Line( var_aclass.Member("foo").Set(var_astruct.Member("foo")) );
			gen.Line( var_astruct.Member("foo").Set(var_buffer) );

			gen.Line( var_buffer.Set(var_aclass.Member("Bar")) );
			gen.Line( var_aclass.Member("Bar").Set(var_astruct.Member("Bar")) );
			gen.Line( var_astruct.Member("Bar").Set(var_buffer) );

			gen.Line( var_aclass.CallMethod("baz") );
			gen.Line( var_astruct.CallMethod("baz") );

			//Cross exchange check
			gen.Line( Expr.CallStatic(typeof(Console), "WriteLine", "Cross change fields...") );
			gen.Line( var_buffer.Set(var_aclass.Member("Bar")) );
			gen.Line( var_aclass.Member("Bar").Set(var_astruct.Member("foo")) );
			gen.Line( var_astruct.Member("foo").Set(var_buffer) );

			gen.Line( var_buffer.Set(var_aclass.Member("foo")) );
			gen.Line( var_aclass.Member("foo").Set(var_astruct.Member("Bar")) );
			gen.Line( var_astruct.Member("Bar").Set(var_buffer) );

			gen.Line( var_aclass.CallMethod("baz") );
			gen.Line( var_astruct.CallMethod("baz") );

			//Cross change

			var func =  gen.compile(true);
			func();
		}



		//Difficult calls crash test
		static AClass t12_aclass = new AClass();
		private static AClass t12_f1() { 
			return t12_aclass;
		}

		static AStruct t12_astruct = new AStruct();
		private static AStruct t12_f2() { 
			return t12_astruct;
		}


		public static void testFunc12() {
			var gen = new ILGen<Action>("TestFunc12", true);

			ILVar var_astruct = gen.DeclareVar(typeof(AStruct));
			gen.Line( var_astruct.Member("foo").Set(11) );
			gen.Line( var_astruct.Member("Bar").Set(12) );
			//t12_f1().GetMe(t12_f1().GetMe(), t12_f1().GetMe()).GetMe().foo = var_astruct.GetMe().GetMe().GetMe().foo;
			gen.Line( Expr.CallStatic(typeof(Program), "t12_f1").CallMethod("GetMe", Expr.CallStatic(typeof(Program), "t12_f1").CallMethod("GetMe"), Expr.CallStatic(typeof(Program), "t12_f1").CallMethod("GetMe")).CallMethod("GetMe").Member("foo").Set(var_astruct.CallMethod("GetMe").CallMethod("GetMe").CallMethod("GetMe").Member("foo")) );
			//t12_f1().GetMe().GetMe().Bar = var_astruct.GetMe().GetMe(var_astruct.GetMe().GetMe(), var_astruct.GetMe().GetMe()).GetMe().GetMe().Bar;
			gen.Line( Expr.CallStatic(typeof(Program), "t12_f1").CallMethod("GetMe").CallMethod("GetMe").Member("Bar").Set(var_astruct.CallMethod("GetMe").CallMethod("GetMe", var_astruct.CallMethod("GetMe").CallMethod("GetMe"), var_astruct.CallMethod("GetMe").CallMethod("GetMe")).CallMethod("GetMe").CallMethod("GetMe").Member("Bar")) );
			gen.Line( Expr.CallStatic(typeof(Program), "t12_f1").Member("baz").call() );

			var func =  gen.compile(true);
			func();
		}

		public static void testFunc13() { 

			var gen = new ILGen<Func<string>>("TestFunc13", true);

			ILVar str = gen.DeclareVar<string>();
			ILVar arr = gen.DeclareVar<int[,]>();
			gen.Line( arr.Set(Expr.InitArray(typeof(int), new Expr[] { 2, 2 })) );
			gen.Line( str.Set(arr.Index(0, 0).CompatiblePass<string>()) );
			gen.Return(str);

			var func =  gen.compile(true);
			func();
		}

		public static void testFunc14() { 
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
			func();
		}

		public static void testGeneric<T>(T d) {
			Console.WriteLine(d);
		}

		public static void testFunc15() { 
			
			var gen = new ILGen<Action>("TestFunc15", true);

			gen.Line(Expr.CallStatic(typeof(Program).GetMethod("testGeneric", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(typeof(int)), 12));

			var func =  gen.compile(true);
			func();
		}

		public static void testFunc16() { 

			var gen = new ILGen<Func<Vector>>("TestFunc16", true);

			gen.Return(Expr.CreateUninitialized(typeof(Vector)));

			var func =  gen.compile(true);
			Console.WriteLine("Uninitialized vector: {0}", func());
		}

		delegate int TestDelegate(int a, int b);

		public static int testAddition(int a, int b) {
			Console.WriteLine($"Call testAddition with args a = {a}, b = {b}");
			return a + b;
		}

		public static void testFunc17() { 
			var gen = new ILGen<Func<int, int, int>>("TestFunc17", true);
			ILVar delegateVar = gen.DeclareVar(typeof(TestDelegate));

			gen.Line(delegateVar.Set(Expr.CreateDelegate(typeof(TestDelegate), Expr.NULL, typeof(Program).GetMethod("testAddition", BindingFlags.Public | BindingFlags.Static))));
			gen.Return(delegateVar.Invoke(gen.args[0], gen.args[1]));

			var func = gen.compile(true);
			int result = func(5, 6);
			Console.WriteLine($"Result: {result}");
		}
		
		public static void testFunc18() { 
			var gen = new ILGen<Func<object, object, bool>>("TestFunc18", true);
			ILVar delegateVar = gen.DeclareVar(typeof(TestDelegate));

			gen.Return(Expr.Equals(gen.args[0], gen.args[1]));

			var func = gen.compile(true);
			Console.WriteLine($"Result: {func(5, 6)}");
			Console.WriteLine($"Result: {func(5, 5)}");
			object o = new object();
			Console.WriteLine($"Result: {func(o, o)}");

		}

		//NULL compare fix
		public static void testFunc19() { 
			var gen = new ILGen<Func<object, bool>>("TestFunc19", true);
			ILVar delegateVar = gen.DeclareVar(typeof(TestDelegate));


			gen.Return(Expr.Equals(gen.args[0], Expr.NULL));

			var func = gen.compile(true);
			object o = new object();
			Console.WriteLine($"Result: {func(o)}");

		}

		//Other integers types consts fix
		public static void testFunc20() { 
			var gen = new ILGen<Func<int>>("TestFunc20", true);
			ILVar delegateVar = gen.DeclareVar(typeof(TestDelegate));


			gen.Return(Expr.Const((byte)20));

			var func = gen.compile(true);
			Console.WriteLine($"Result: {func()}");

		}

		//Other integers types consts fix
		public static void testFunc21() { 
			var gen = new ILGen<Func<int>>("TestFunc21", true);
			ILVar delegateVar = gen.DeclareVar(typeof(TestDelegate));


			gen.Return(Expr.Const((byte)20));

			var func = gen.compile(true);
			Console.WriteLine($"Result: {func()}");

		}

		//Return fix
		public static void testFunc22() { 
			var gen = new ILGen<Func<object, int>>("TestFunc22", true);
			ILVar delegateVar = gen.DeclareVar(typeof(TestDelegate));

			gen.If(Expr.Equals(gen.args[0], Expr.NULL));
				gen.Return(Expr.Const(0));
			gen.EndIf();

			gen.Return(Expr.Const(1));

			var func = gen.compile(true);
			Console.WriteLine($"Result: {func(new object())}");
			Console.WriteLine($"Result: {func(null)}");

		}

		public static void testFunc23() {
			var gen = new ILGen<Func<StructVector, double>>("TestFunc23", true);
			ILVar testStructVector = gen.DeclareVar<StructVector>();
			
			gen.Line(testStructVector.Set(Expr.NewObject<StructVector>(Expr.Add(gen.args[0].Field("x"), gen.args[0].Field("y")), Expr.Mul(gen.args[0].Field("x"), gen.args[0].Field("y")))));

			gen.Return(Expr.Add(testStructVector.Field("x"), testStructVector.Field("y")));

			var func = gen.compile(true);
			//4 + 5 = 9; 4 * 5 = 20; 9 + 20 = 29
			Console.WriteLine($"Result: {func(new StructVector(4, 5))}");
		}

		public static void testFunc24() {
			var gen = new ILGen<Func<(double, double), double>>("TestFunc24", true);
			ILVar testTuple = gen.DeclareVar<(double, double)>();
			
			gen.Line(testTuple.Set(Expr.NewObject<(double, double)>(Expr.Add(gen.args[0].Field("Item1"), gen.args[0].Field("Item2")), Expr.Mul(gen.args[0].Field("Item1"), gen.args[0].Field("Item2")))));

			gen.Return(Expr.Add(testTuple.Field("Item1"), testTuple.Field("Item2")));

			var func = gen.compile(true);
			//4 + 5 = 9; 4 * 5 = 20; 9 + 20 = 29
			Console.WriteLine($"Result: {func((4, 5))}");
		}
	}

	

}
