using InSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpeedTest {
	public class Program {
		//Utils

		private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static long CurrentTimeMicros() {
			return (long) (DateTime.UtcNow - Jan1st1970).Ticks / (TimeSpan.TicksPerMillisecond / 1000);
		}

		private static long fixedTime1, fixedTime2;

		private static void FixTime1() => fixedTime1 = CurrentTimeMicros();
		private static void FixTime2() => fixedTime2 = CurrentTimeMicros();
		private static void PrintTime(string str) {
			long time2 = CurrentTimeMicros();
			Console.WriteLine($"{str}: {fixedTime2 - fixedTime1} microseconds");
		}




		public static void Main(string[] args) {
			TestClass test = new TestClass { A = 100, B = 500, C = 1000, str1 = "Hello ", str2 = "world!"};

			FixTime1();
			for(int counter = 0; counter < 10000000; counter++) {
				SumAll_Compiled(test);
			}
			FixTime2();
			PrintTime("Compiled execution test time");
			Console.WriteLine();

			FixTime1();
			Func<TestClass, string> func_insharp = InSharpBuild<TestClass>();
			FixTime2();
			PrintTime("InSharp build");
			Console.WriteLine($"InSharp Result: {func_insharp(test)}");
			FixTime1();
			for(int counter = 0; counter < 10000000; counter++) {
				func_insharp(test);
			}
			FixTime2();
			PrintTime("InSharp execution test time");
			Console.WriteLine();

			FixTime1();
			Func<TestClass, string> func_expr = ExprBuild<TestClass>();
			FixTime2();
			PrintTime("Expr build");
			Console.WriteLine($"Expr Result: {func_expr(test)}");
			FixTime1();
			for(int counter = 0; counter < 10000000; counter++) {
				func_expr(test);
			}
			FixTime2();
			PrintTime("Expr execution test time");
			Console.WriteLine();
			

			

			Console.ReadKey();
		}
		// Try to build an universal method for sum of all int fields of the given type, concatenate string fields and concatenate it with the result int
		// Method for TestClass  
		public static string SumAll_Compiled(TestClass obj) {
			return (obj.str1 + obj.str2 + "; " + (obj.A + obj.B + obj.C));
		}

		public static Func<T, string> InSharpBuild<T>() { 

			var gen = new ILGen<Func<T, string>>($"{typeof(T).Name}_sum_func", true);

			ILVar intSum = gen.DeclareVar<int>();

			Expr intSumExpr = null;
			Expr strSumExpr = null;
			foreach(MemberInfo memberInfo in typeof(T).GetMembers().OrderBy((MemberInfo m) => m.MetadataToken)) { 
				if(memberInfo.MemberType != MemberTypes.Field && memberInfo.MemberType != MemberTypes.Property)
					continue;
				Type memberType = memberInfo.MemberType == MemberTypes.Field ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType;
				if(memberType == typeof(int))
					intSumExpr = intSumExpr == null ? gen.args[0].Member(memberInfo) : Expr.Add(intSumExpr, gen.args[0].Member(memberInfo));
				if(memberType == typeof(string))
					strSumExpr = strSumExpr == null ? gen.args[0].Member(memberInfo) : Expr.Add(strSumExpr, gen.args[0].Member(memberInfo));
			}

			gen.Return(Expr.Add(Expr.Add(strSumExpr, new ILConst("; ")), intSumExpr.CallMethod(typeof(int).GetMethod("ToString", new Type[] { }))));

			return gen.compile();
		}


		public static Func<T, string> ExprBuild<T>() { 
			ParameterExpression objectParam = Expression.Parameter(typeof(T));
			

			MethodInfo concatMethod = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) });
			Expression strSumExpr = null;
			foreach(MemberInfo memberInfo in typeof(T).GetMembers().Where(m => m.MemberType == MemberTypes.Field ? ((FieldInfo)m).FieldType == typeof(string) : m.MemberType == MemberTypes.Property ? ((PropertyInfo)m).PropertyType == typeof(string) : false).OrderBy((MemberInfo m) => m.MetadataToken)) { 
				Expression memberExpression = memberInfo.MemberType == MemberTypes.Property ? Expression.Property(objectParam, (PropertyInfo)memberInfo) : Expression.Field(objectParam, (FieldInfo)memberInfo);
				strSumExpr = strSumExpr == null ? memberExpression : Expression.Call(null, concatMethod, strSumExpr, memberExpression);
			}

			Expression intSumExpr = null;
			foreach(MemberInfo memberInfo in typeof(T).GetMembers().Where(m => m.MemberType == MemberTypes.Field ? ((FieldInfo)m).FieldType == typeof(int) : m.MemberType == MemberTypes.Property ? ((PropertyInfo)m).PropertyType == typeof(int) : false).OrderBy((MemberInfo m) => m.MetadataToken)) { 
				Expression memberExpression = memberInfo.MemberType == MemberTypes.Property ? Expression.Property(objectParam, (PropertyInfo)memberInfo) : Expression.Field(objectParam, (FieldInfo)memberInfo);
				intSumExpr = intSumExpr == null ? memberExpression : Expression.Add(intSumExpr, memberExpression);
			}

			MethodInfo intToStringMethod = typeof(int).GetMethod("ToString", new Type[] { });
			return Expression.Lambda<Func<T, string>>(Expression.Call(concatMethod, Expression.Call(concatMethod, strSumExpr, Expression.Constant("; ", typeof(string))), Expression.Call(intSumExpr, intToStringMethod)), objectParam).Compile();
		}


	}

	public class TestClass { 
		public int A;
		public int B { get; set; }
		public int C;
		public string str1;
		public string str2 { get; set; }
	}
}
