using LowLevelOpsHelper;
using ShowCode_Casts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ShowCode {

	class AClass { 
		public int foo;
		public AStruct astruct;
		public int MyProperty { get; }

		public void bar() { }

		public int baz() { return 0; }

		public int alpha() { return 1; }

		public AClass beta() { return this; }

		public void gamma(int i) { }

		public int this[int index] {
			get {
				return 0;
			}
			set {

			}
		}

	}

	class BClass { 
		public int foo;
		
	}

	struct AStruct { 
		public BStruct bstruct;
		public AClass aclass;

		public int foo;

		public int MyField { get; set; }

		public void bar() { }
		
		public AStruct baz() { return new AStruct(); }

		public AStruct(int a = 0) { 
			foo = a;
			MyField = a;
			bstruct = new BStruct();
			aclass = null;
		}

		public void alpha(int a) {
		
		}

		public int getFoo() { 
			return foo;
		}

		public int getFoo(int a) { 
			return foo;
		}

		public int this[int index] {
			get {
				return 0;
			}
			set {

			}
		}
	}

	struct BStruct { 
		public int foo;

		public void bar() { }
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

	class Program {
		static void Main(string[] args) {
			//CastTest.Test();
			/*
			foreach(FieldInfo fieldInfo in typeof(AClass).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) { 
				Console.WriteLine("Field: {0}", fieldInfo.Name);
			}*/
			foreach(ConstructorInfo constructor in typeof(D).GetConstructors())
				Console.WriteLine($"Constructor: {constructor}");
			Console.ReadKey();
		}

		static int func1(int a, int b) {
			int c = a + b;
			Console.WriteLine(c);
			return c;
		}

		static void func2(AClass aclass, AStruct astruct) { 
			aclass.foo = astruct.foo;
			astruct.foo = aclass.foo;
		}

		static void func3(AClass aclass, AStruct astruct) { 
			aclass.bar();
			astruct.bar();
		}

		static AStruct func4() { 
			return new AStruct();
		}

		static void func5() { 
			func4().baz().baz().bar();

			AClass aclass = new AClass();
			aclass.baz();
			aclass.alpha();

			func4();

			func6(new AStruct(), out int i);

			Console.WriteLine(func4().bstruct.foo);
		}

		static void func6(AStruct astruct, out int o) { 
			o = astruct.foo;
			astruct.bar();


		}

		static void func7(AClass aclass) {
			aclass.beta().beta().foo = 12;

			func4().bstruct.bar();

			aclass.gamma(100500);
		}

		static AClass func8() { 
			return new AClass();
		}

		static void func9() { 
			func4().aclass.bar();

			func8().astruct.bar();
		}

		static int func10(int a, AStruct b) {
			a = b.foo++;

			return a;
		}

		static AStruct func11() {
			AStruct astruct =default(AStruct);

			astruct.aclass = func8();

			return astruct;
		}

		/*static sbyte func12(sbyte i1, short i2, int i4, long i8, float f1, double f2) {
			return i1 + i1;
		}

		static uint func13(sbyte i1, short i2, int i4, long i8, byte u1, ushort u2, uint u4, ulong u8, float f1, double f2) {
			return u1 + u1;
		}*/

		static short func14(float l) { 
			return (short)l;
		}

		static int[] func15(int arg1, int arg2, int arg3) {
			int[] result = { arg1, arg2, arg3};

			return result;
		}

		static int func16(int a, int b) { 
			return a % b;
		}

		static int func17(int a, int b) { 
			return a >> b;
		}

		static long func18(long a, int b) { 
			return a & b;
		}

		static bool func19(int a, long b) { 
			return a == b;
		}

		static int func20(int a) { 
			return ~a;
		}

		static bool func21(bool a) { 
			return !a;
		}

		static int func22(AStruct a) { 
			return ++a.foo;
		}

		static void func23() {
			func20(func11().foo);
		}

		static int func24() {
			return 100500;
		}

		static BClass func25() { 
			return new BClass();
		}

		static void func26() { 
			func25().foo = (func8().foo = func24());
		}

		static void func27() { 
			func11().alpha(func11().getFoo(func11().getFoo()));
		}

		static void func28() { 
			func11().baz().baz().baz();
		}

		static void func29() { 
			AStruct s = func11();
			s.MyField = 12;
		}

		static int[] func30(int w) { 
			return new int[w];
		}

		static int[,] func31(long w, long h) { 
			return new int[w, h];
		}
	
		static int func32(int[,] arr, int a, int b) {
			return arr[a, b];
		}

		static int func33(AClass aclass) { 
			return aclass.MyProperty;
		}

		static int func34(int[,] arr) { 
			return arr.GetLength(1);
		}

		static int func35(int[,] arr) { 
			return arr.Length;
		}

		static int func36(int[] arr) { 
			return arr.GetLength(0);
		}

		static int func37(int[] arr) { 
			return arr.Length;
		}

		static int func38(int[] arr) { 
			return arr.Rank;
		}

		static void func39(int[,] arr) { 
			arr[2, 3] = 122;
		}

		static bool func40(AClass obj) { 
			return obj == null;
		}

		static bool func41(AClass obj1, AClass obj2) { 
			return obj1 == obj2;
		}

		static AStruct func42() { 
			return new AStruct(12);
		}



		static int func43(AClass a) { 
			return a[2];
		}

		static string func44(string a, int b) { 
			return a + b;
		}

		static string func45() { 
			string a = "";
			int[,] b = new int[2, 2];
			return a + b[0, 0];
		}

		static string func45_2(string a, int[,] b) {
			return a + b[1, 0];
		}

		static string func45_3(string a, int[] b) {
			return a + b[1];
		}

		static string func45_4(string a, object[,] b) {
			return a + b[1, 1];
		}


		static string func45_5(string a, object[] b) {
			return a + b[1];
		}

		static void func46() { 
			int[,] matrix = new int[3, 3];
			matrix[0, 0] = 1;
			matrix[0, 1] = 2;
			matrix[0, 2] = 1;

			matrix[1, 0] = 2;
			matrix[1, 1] = 4;
			matrix[1, 2] = 2;

			matrix[2, 0] = 1;
			matrix[2, 1] = 2;
			matrix[2, 2] = 1;
			int index1 = 0;
			int index2 = 0;
			
			while(index1 < matrix.GetLength(0)) {
				string line = "";
				while(index2 < matrix.GetLength(1)) {
					line = ((line + "; ") + matrix[index1, index2]);
					index2 = index2 + 1;
				}
				index1 = index1 + 1;
			}
		}

		static void genericFunc47<T>(T obj) {
			Console.WriteLine(obj);
		}

		static void func47() {
			genericFunc47(88);
		}

		static AClass func48() {
			return (AClass)FormatterServices.GetUninitializedObject(typeof(AClass));
		}

		static object func49_func(int a, object b) { 
			return null;
		}

		delegate object D(int a, object b);
		static void func49() { 
			D myDelegate = new D(func49_func);
			object result = myDelegate(5, 25);
		}

		static bool func50(object obj1, object obj2) {
			return obj1 == obj2;
		}

		static bool func51(int a, short b) { 
			return a == b;
		}

		static int func52(object a) {
			if(a == null)
				return 0;

			int i1 = 3;
			int i2 = 4;
			int i3 = 5;
			int buffer;
			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			buffer = i3;
			i3 = i2;
			i2 = i1;
			i1 = buffer;

			return i1;
		}

		static void func53_func(ref int i) { 
		}

		static void func53() { 
			int i = 3;
			func53_func(ref i);
		}

		static void func54_func(ref object i) { 
		}

		static void func54() { 
			object o = new object();
			func54_func(ref o);
		}

		static double func55(StructVector vec1) { 
			StructVector v = new StructVector(vec1.x + vec1.y, vec1.x * vec1.y);
			return v.x + v.y;
		}

		static int func56((int a, int b) t1) { 
			(int c, int d) t2 = (t1.a + t1.b, t1.a * t1.b);
			return t2.c + t2.d;
		}

		static string func57(int a) { 
			return a.ToString();
		}
	}
}
