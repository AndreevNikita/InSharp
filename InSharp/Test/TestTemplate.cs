using InSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Test {
	public class CallMethodsCheck {

		public static void StaticAction(int a, int b) { Console.WriteLine("Static action {0}", a + b); }
		public static void MemberAction() {}


	}
	static class TestTemplate {

		public static void Test() {
			Console.WriteLine("Test call templates");
			Func<object, object[], object> staticFuncCaller = ILTemplate.CommonFunctionCallShell(typeof(CallMethodsCheck).GetMethod("StaticAction", BindingFlags.Static | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null));
			staticFuncCaller(null, new object[] {2, 3});
		}

		public static object test_func_1(object obj, object[] args) {
			CallMethodsCheck.StaticAction((int)args[0], (int)args[1]);
			return null;
		}

	}
}
