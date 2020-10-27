using InSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Test {
	public class TestClass {

		public static int StaticField = 100500;
		public static int StaticProperty { get; set; } = 1050;

		public int InstanceField = 500100;
		public int InstanceProperty { get; set; } = 5010;

		public static void StaticAction(int a, int b) { Console.WriteLine("Static action {0}", a + b); }
		public static int StaticFunction(int a, int b) { Console.WriteLine("Static function {0}", a + b); return a + b; }


		public void InstanceAction(int a, int b) { Console.WriteLine("Instance action {0}", a + b); }
		public int InstanceFunction(int a, int b) { Console.WriteLine("Instance function {0}", a + b); return a + b; }

		public override string ToString() {
			return $"{base.ToString()}: InstanceField = {InstanceField}, InstanceProperty = {InstanceProperty}";
		}

	}
	static class TestTemplate {

		public static void Test() {
			Console.WriteLine("Test call templates");
			Console.WriteLine();
			
			TestClass classInstance = new TestClass();
			//Test members get/set shells
			Func<object, object> commonMemberGetter;
			Action<object, object> commonMmeberSetter;

			//--------------------------------Getters--------------------------------

			//Common

			commonMemberGetter = ILTemplate.CommonMemberGetter(typeof(TestClass).GetMember("StaticField", BindingFlags.Public | BindingFlags.Static)[0], null, true);
			Console.WriteLine($"Get StaticField: {commonMemberGetter(null)}");
			commonMemberGetter = ILTemplate.CommonMemberGetter(typeof(TestClass).GetMember("StaticProperty", BindingFlags.Public | BindingFlags.Static)[0], null, true);
			Console.WriteLine($"Get StaticProperty: {commonMemberGetter(null)}");

			commonMemberGetter = ILTemplate.CommonMemberGetter(typeof(TestClass).GetMember("InstanceField", BindingFlags.Public | BindingFlags.Instance)[0], null, true);
			Console.WriteLine($"Get MemberField: {commonMemberGetter(classInstance)}");
			commonMemberGetter = ILTemplate.CommonMemberGetter(typeof(TestClass).GetMember("InstanceProperty", BindingFlags.Public | BindingFlags.Instance)[0], null, true);
			Console.WriteLine($"Get MemberProperty: {commonMemberGetter(classInstance)}");

			//Static

			var staticMemberGetter = ILTemplate.StaticMemberGetter(typeof(TestClass).GetMember("StaticField", BindingFlags.Public | BindingFlags.Static)[0], null, true);
			Console.WriteLine($"Get StaticField: {staticMemberGetter()}");
			staticMemberGetter = ILTemplate.StaticMemberGetter(typeof(TestClass).GetMember("StaticProperty", BindingFlags.Public | BindingFlags.Static)[0], null, true);
			Console.WriteLine($"Get StaticProperty: {staticMemberGetter()}");

			//Instance

			var instanceMemberGetter = ILTemplate.InstanceMemberGetter(typeof(TestClass).GetMember("InstanceField", BindingFlags.Public | BindingFlags.Instance)[0], null, true);
			Console.WriteLine($"Get MemberField: {instanceMemberGetter(classInstance)}");
			instanceMemberGetter = ILTemplate.InstanceMemberGetter(typeof(TestClass).GetMember("InstanceProperty", BindingFlags.Public | BindingFlags.Instance)[0], null, true);
			Console.WriteLine($"Get MemberProperty: {instanceMemberGetter(classInstance)}");

			//--------------------------------Setters--------------------------------

			//Common
			commonMmeberSetter = ILTemplate.CommonMemberSetter(typeof(TestClass).GetMember("StaticField", BindingFlags.Public | BindingFlags.Static)[0], null, true);
			commonMmeberSetter(null, 1005002);
			Console.WriteLine($"New StaticField value: {TestClass.StaticField}");

			commonMmeberSetter = ILTemplate.CommonMemberSetter(typeof(TestClass).GetMember("StaticProperty", BindingFlags.Public | BindingFlags.Static)[0], null, true);
			commonMmeberSetter(null, 10502);
			Console.WriteLine($"New StaticProperty value: {TestClass.StaticProperty}");

			commonMmeberSetter = ILTemplate.CommonMemberSetter(typeof(TestClass).GetMember("InstanceField", BindingFlags.Public | BindingFlags.Instance)[0], null, true);
			commonMmeberSetter(classInstance, 5001002);
			Console.WriteLine($"New InstanceField value: {classInstance.InstanceField}");

			commonMmeberSetter = ILTemplate.CommonMemberSetter(typeof(TestClass).GetMember("InstanceProperty", BindingFlags.Public | BindingFlags.Instance)[0], null, true);
			commonMmeberSetter(classInstance, 50102);
			Console.WriteLine($"New InstanceProperty value: {classInstance.InstanceProperty}");

			//Static

			var staticMemberSetter = ILTemplate.StaticMemberSetter(typeof(TestClass).GetMember("StaticField", BindingFlags.Public | BindingFlags.Static)[0], null, true);
			staticMemberSetter(1005003);
			Console.WriteLine($"New StaticField value: {TestClass.StaticField}");

			staticMemberSetter = ILTemplate.StaticMemberSetter(typeof(TestClass).GetMember("StaticProperty", BindingFlags.Public | BindingFlags.Static)[0], null, true);
			staticMemberSetter(10503);
			Console.WriteLine($"New StaticProperty value: {TestClass.StaticProperty}");

			//Instance

			var instanceMemberSetter = ILTemplate.InstanceMemberSetter(typeof(TestClass).GetMember("InstanceField", BindingFlags.Public | BindingFlags.Instance)[0], null, true);
			instanceMemberSetter(classInstance, 1005003);
			Console.WriteLine($"New InstanceField value: {classInstance.InstanceField}");

			instanceMemberSetter = ILTemplate.InstanceMemberSetter(typeof(TestClass).GetMember("InstanceProperty", BindingFlags.Public | BindingFlags.Instance)[0], null, true);
			instanceMemberSetter(classInstance, 10503);
			Console.WriteLine($"New InstanceProperty value: {classInstance.InstanceProperty}");

			//--------------------------------Construcrot call--------------------------------

			var objectCreator = ILTemplate.CommonObjectCreator(typeof(TestClass).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null), null, true);
			Console.WriteLine($"New object create: {objectCreator(new object[] { })}");
			
			//--------------------------------Methods call--------------------------------

			Console.WriteLine("Test common call shell");
			Func<object, object[], object> commonFuncCaller;
			int result;
			commonFuncCaller = ILTemplate.CommonCallShell(typeof(TestClass).GetMethod("StaticAction", BindingFlags.Static | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			commonFuncCaller(null, new object[] {2, 3});

			commonFuncCaller = ILTemplate.CommonCallShell(typeof(TestClass).GetMethod("StaticFunction", BindingFlags.Static | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			result = (int)commonFuncCaller(null, new object[] {2, 3});
			Console.WriteLine($"Result: {result}");

			commonFuncCaller = ILTemplate.CommonCallShell(typeof(TestClass).GetMethod("InstanceAction", BindingFlags.Instance | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			commonFuncCaller(classInstance, new object[] {2, 3});

			commonFuncCaller = ILTemplate.CommonCallShell(typeof(TestClass).GetMethod("InstanceFunction", BindingFlags.Instance | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			result = (int)commonFuncCaller(classInstance, new object[] {2, 3});
			Console.WriteLine($"Result: {result}");



			Console.WriteLine();
			Console.WriteLine("Test special shells");
			Func<object[], object> commonStaticCaller;
			commonStaticCaller = ILTemplate.CommonStaticCallShell(typeof(TestClass).GetMethod("StaticAction", BindingFlags.Static | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			commonStaticCaller(new object[] {2, 3});

			commonStaticCaller = ILTemplate.CommonStaticCallShell(typeof(TestClass).GetMethod("StaticFunction", BindingFlags.Static | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			result = (int)commonStaticCaller(new object[] {2, 3});
			Console.WriteLine($"Result: {result}");

			var staticActionCaller = ILTemplate.StaticActionCallShell(typeof(TestClass).GetMethod("StaticAction", BindingFlags.Static | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			staticActionCaller(new object[] {2, 3});

			var staticFunctionCaller = ILTemplate.StaticFunctionCallShell(typeof(TestClass).GetMethod("StaticFunction", BindingFlags.Static | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			result = (int)staticFunctionCaller(new object[] {2, 3});
			Console.WriteLine($"Result: {result}");


			Func<object, object[], object> commonMemberCaller;
			commonMemberCaller = ILTemplate.CommonInstanceCallShell(typeof(TestClass).GetMethod("InstanceAction", BindingFlags.Instance | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			commonMemberCaller(classInstance, new object[] {2, 3});

			commonMemberCaller = ILTemplate.CommonInstanceCallShell(typeof(TestClass).GetMethod("InstanceFunction", BindingFlags.Instance | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			result = (int)commonMemberCaller(classInstance, new object[] {2, 3});
			Console.WriteLine($"Result: {result}");

			var instanceActionCaller = ILTemplate.InstanceActionCallShell(typeof(TestClass).GetMethod("InstanceAction", BindingFlags.Instance | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			instanceActionCaller(classInstance, new object[] {2, 3});

			var instanceFunctionCaller = ILTemplate.InstanceFunctionCallShell(typeof(TestClass).GetMethod("InstanceFunction", BindingFlags.Instance | BindingFlags.Public, null, new []{typeof(int), typeof(int)}, null), null, true);
			result = (int)instanceFunctionCaller(classInstance, new object[] {2, 3});
			Console.WriteLine($"Result: {result}");


		}
		
	}
}
