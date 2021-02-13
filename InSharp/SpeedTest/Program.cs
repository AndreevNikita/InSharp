using InSharp;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
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
		private static void PrintTime(TimeSpan time, string str) {
			long time2 = CurrentTimeMicros();
			Console.WriteLine($"{str}: {time.Ticks / (TimeSpan.TicksPerMillisecond / 1000)} microseconds");
		}


		private static string CompilerTestFunc<T>(Func<Func<T, string>> compile, T testObject) { 
			long compilationTime, executionTime;

			Stopwatch timer = new Stopwatch();
			//Compilation measurement
			timer.Start();
			Func<T, string> testFunc = compile();
			timer.Stop();
			compilationTime = timer.Elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000);

			timer.Reset();

			//Execution measurement
			timer.Start();
			for(int counter = 0; counter < 10000000; counter++) {
				testFunc(testObject);
			}
			timer.Stop();
			executionTime = timer.Elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000);


			return $"Test output: \"{testFunc(testObject)}\"\nCompilation time: {compilationTime} microseconds\nExecution time: {executionTime} microseconds";

		}

		public static void Main(string[] args) {
			TestClass testObject = new TestClass { A = 100, B = 500, C = 1000, str1 = "Hello ", str2 = "world!"};
			Task<string> compiledTestTask, inSharpTestTask, exprTestTask, codeProviderTTestask;

			//--------------------------------Async test--------------------------------

			compiledTestTask =		new Task<string>(() => CompilerTestFunc(() => SumAll_Compiled, testObject));
			inSharpTestTask =		new Task<string>(() => CompilerTestFunc(() => InSharpCompile<TestClass>(), testObject));
			exprTestTask =			new Task<string>(() => CompilerTestFunc(() => ExprCompile<TestClass>(), testObject));
			codeProviderTTestask =	new Task<string>(() => CompilerTestFunc(() => CodeproviderCompile<TestClass>(), testObject));
			

			compiledTestTask.Start();
			inSharpTestTask.Start();
			exprTestTask.Start();
			codeProviderTTestask.Start();

			Task.WaitAll(compiledTestTask, exprTestTask, inSharpTestTask, codeProviderTTestask);

			Console.WriteLine("Async Test:");
			Console.WriteLine($"Compiled:\n{compiledTestTask.Result}\n");
			Console.WriteLine($"InSharp:\n{inSharpTestTask.Result}\n");
			Console.WriteLine($"Expression tree:\n{exprTestTask.Result}\n");
			Console.WriteLine($"CodeProvider:\n{codeProviderTTestask.Result}\n");
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();

			//--------------------------------Sync test--------------------------------

			compiledTestTask =		new Task<string>(() => CompilerTestFunc(() => SumAll_Compiled, testObject));
			inSharpTestTask =		new Task<string>(() => CompilerTestFunc(() => InSharpCompile<TestClass>(), testObject));
			exprTestTask =			new Task<string>(() => CompilerTestFunc(() => ExprCompile<TestClass>(), testObject));
			codeProviderTTestask =	new Task<string>(() => CompilerTestFunc(() => CodeproviderCompile<TestClass>(), testObject));
			

			compiledTestTask.RunSynchronously();
			inSharpTestTask.RunSynchronously();
			exprTestTask.RunSynchronously();
			codeProviderTTestask.RunSynchronously();

			Task.WaitAll(compiledTestTask, exprTestTask, inSharpTestTask, codeProviderTTestask);

			Console.WriteLine("Sync Test:");
			Console.WriteLine($"Compiled:\n{compiledTestTask.Result}\n");
			Console.WriteLine($"InSharp:\n{inSharpTestTask.Result}\n");
			Console.WriteLine($"Expression tree:\n{exprTestTask.Result}\n");
			Console.WriteLine($"CodeProvider:\n{codeProviderTTestask.Result}\n");

			Console.ReadKey();
		}
		// Try to build an universal method for sum of all int fields of the given type, concatenate string fields and concatenate it with the result int
		// Method for TestClass  
		public static string SumAll_Compiled(TestClass obj) {
			return (obj.str1 + obj.str2 + "; " + (obj.A + obj.B + obj.C));
		}

		public static Func<T, string> InSharpCompile<T>() { 

			var gen = new ILGen<Func<T, string>>($"{typeof(T).Name}_sum_func", true);

			ILVar intSum = gen.DeclareVar<int>();

			Expr intSumExpr = null;
			Expr strSumExpr = null;
			foreach(MemberInfo memberInfo in typeof(T).GetMembers().Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property).OrderBy((MemberInfo m) => m.MetadataToken)) { 
				Type memberType = memberInfo.MemberType == MemberTypes.Field ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType;
				if(memberType == typeof(int))
					intSumExpr = intSumExpr == null ? gen.args[0].Member(memberInfo) : Expr.Add(intSumExpr, gen.args[0].Member(memberInfo));
				else if(memberType == typeof(string))
					strSumExpr = strSumExpr == null ? gen.args[0].Member(memberInfo) : Expr.Add(strSumExpr, gen.args[0].Member(memberInfo));
			}

			gen.Return(Expr.Add(Expr.Add(strSumExpr, new ILConst("; ")), intSumExpr.CallMethod(typeof(int).GetMethod("ToString", new Type[] { }))));

			return gen.compile();
		}

		public static Func<T, string> ExprCompile<T>() { 
			ParameterExpression objectParam = Expression.Parameter(typeof(T));
			

			MethodInfo concatMethod = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) });
			Expression strSumExpr = null;
			Expression intSumExpr = null;
			foreach(MemberInfo memberInfo in typeof(T).GetMembers().Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property).OrderBy((MemberInfo m) => m.MetadataToken)) { 
				Type memberType = memberInfo.MemberType == MemberTypes.Field ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType;
				if(memberType == typeof(int)) {
					Expression memberExpression = memberInfo.MemberType == MemberTypes.Property ? Expression.Property(objectParam, (PropertyInfo)memberInfo) : Expression.Field(objectParam, (FieldInfo)memberInfo);
					intSumExpr = intSumExpr == null ? memberExpression : Expression.Add(intSumExpr, memberExpression);
				} else if(memberType == typeof(string)) {
					Expression memberExpression = memberInfo.MemberType == MemberTypes.Property ? Expression.Property(objectParam, (PropertyInfo)memberInfo) : Expression.Field(objectParam, (FieldInfo)memberInfo);
					strSumExpr = strSumExpr == null ? memberExpression : Expression.Call(null, concatMethod, strSumExpr, memberExpression);
				}
			}

			MethodInfo intToStringMethod = typeof(int).GetMethod("ToString", new Type[] { });
			return Expression.Lambda<Func<T, string>>(Expression.Call(concatMethod, Expression.Call(concatMethod, strSumExpr, Expression.Constant("; ", typeof(string))), Expression.Call(intSumExpr, intToStringMethod)), objectParam).Compile();
		}

		public static Func<T, string> CodeproviderCompile<T>() { 
		
			string strSumString = null, intSumString = null;

			foreach(MemberInfo memberInfo in typeof(T).GetMembers().OrderBy((MemberInfo m) => m.MetadataToken)) { 
				if(memberInfo.MemberType != MemberTypes.Field && memberInfo.MemberType != MemberTypes.Property)
					continue;
				Type memberType = memberInfo.MemberType == MemberTypes.Field ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType;
				if(memberType == typeof(int))
					intSumString += intSumString == null ? $"obj.{memberInfo.Name}" : $" + obj.{memberInfo.Name}";
				if(memberType == typeof(string))
					strSumString += strSumString == null ? $"obj.{memberInfo.Name}" : $" + obj.{memberInfo.Name}";
			}


			var code = $@"

				public static class {typeof(T).Name}_SumWrapper {{
				   
					public static string SumFunc({typeof(T).FullName} obj) {{
						return {strSumString} + {"\"; \""} + ( {intSumString} );
					}}

				}}";

			var options = new CompilerParameters();
			options.GenerateExecutable = false;
			options.GenerateInMemory = false;

			options.ReferencedAssemblies.Add("System.dll");
			options.ReferencedAssemblies.Add("System.dll");
			options.ReferencedAssemblies.Add("System.Data.dll");
			options.ReferencedAssemblies.Add("System.Xml.dll");
			options.ReferencedAssemblies.Add("mscorlib.dll");
			options.ReferencedAssemblies.Add("System.Windows.Forms.dll");
			options.ReferencedAssemblies.Add(AppDomain.CurrentDomain.FriendlyName);

			var provider = new CSharpCodeProvider();
			var compile = provider.CompileAssemblyFromSource(options, code);

			foreach(var compileError in compile.Errors) { 
				Console.WriteLine(compileError.ToString());
			}

			var type = compile.CompiledAssembly.GetType($"{typeof(T).Name}_SumWrapper");

			return (Func<T, string>)type.GetMethod("SumFunc").CreateDelegate(typeof(Func<T, string>));
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
