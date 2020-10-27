using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace InSharp {
	public static class ILTemplate {

		//Fields & properties

		private static void GetFieldOrPropertyInfo(MemberInfo memberInfo, out bool isStatic, out Type valueType) {
			if(memberInfo is FieldInfo) { 
				isStatic = ((FieldInfo)memberInfo).IsStatic;
				valueType = ((FieldInfo)memberInfo).FieldType;
			} else if(memberInfo is PropertyInfo) { 
				isStatic = ((PropertyInfo)memberInfo).GetAccessors(true).Any(methodInfo => methodInfo.IsStatic);
				valueType = ((PropertyInfo)memberInfo).PropertyType;
			} else { 
				throw new InSharpException($"Member \"{memberInfo}\" is not field or property");
			}
		}

		public static Func<object, object> CommonMemberGetter(MemberInfo memberInfo, string name = null, bool enableDebug = false) {
			
			if(name == null) {
				name = memberInfo.Name + "_Getter";
			}

			var gen = new ILGen<Func<object, object>>(name, true);
			GetFieldOrPropertyInfo(memberInfo, out bool isStatic, out Type valueType);
			if(isStatic) {
				gen.Return( Expr.StaticMember(memberInfo) );
			} else {
				gen.Return( gen.args[0].CompatiblePass(memberInfo.DeclaringType).Member(memberInfo) );
			}

			return gen.compile(enableDebug);
		} 

		public static Func<object> StaticMemberGetter(MemberInfo memberInfo, string name = null, bool enableDebug = false) {
			
			if(name == null) {
				name = memberInfo.Name + "_Getter";
			}

			var gen = new ILGen<Func<object>>(name, true);
			GetFieldOrPropertyInfo(memberInfo, out bool isStatic, out Type valueType);
			gen.Return( Expr.StaticMember(memberInfo) );


			return gen.compile(enableDebug);
		} 

		public static Func<object, object> InstanceMemberGetter(MemberInfo memberInfo, string name = null, bool enableDebug = false) {
			
			if(name == null) {
				name = memberInfo.Name + "_Getter";
			}

			var gen = new ILGen<Func<object, object>>(name, true);
			GetFieldOrPropertyInfo(memberInfo, out bool isStatic, out Type valueType);
			gen.Return( gen.args[0].CompatiblePass(memberInfo.DeclaringType).Member(memberInfo) );

			return gen.compile(enableDebug);
		} 



		public static Action<object, object> CommonMemberSetter(MemberInfo memberInfo, string name = null, bool enableDebug = false) {
			
			if(name == null) {
				name = memberInfo.Name + "_Setter";
			}

			var gen = new ILGen<Action<object, object>>(name, true);
			GetFieldOrPropertyInfo(memberInfo, out bool isStatic, out Type valueType);
			if(isStatic) {
				gen.Line( Expr.StaticMember(memberInfo).Set(gen.args[1].CompatiblePass(valueType)) );
			} else {
				gen.Line( gen.args[0].CompatiblePass(memberInfo.DeclaringType).Member(memberInfo).Set(gen.args[1].CompatiblePass(valueType)) );
			}

			return gen.compile(enableDebug);
		} 

		public static Action<object> StaticMemberSetter(MemberInfo memberInfo, string name = null, bool enableDebug = false) {
			
			if(name == null) {
				name = memberInfo.Name + "_Setter";
			}

			var gen = new ILGen<Action<object>>(name, true);
			GetFieldOrPropertyInfo(memberInfo, out bool isStatic, out Type valueType);
			gen.Line( Expr.StaticMember(memberInfo).Set(gen.args[0].CompatiblePass(valueType)) );
			
			return gen.compile(enableDebug);
		} 

		public static Action<object, object> InstanceMemberSetter(MemberInfo memberInfo, string name = null, bool enableDebug = false) {
			
			if(name == null) {
				name = memberInfo.Name + "_Setter";
			}

			var gen = new ILGen<Action<object, object>>(name, true);
			GetFieldOrPropertyInfo(memberInfo, out bool isStatic, out Type valueType);
			gen.Line( gen.args[0].CompatiblePass(memberInfo.DeclaringType).Member(memberInfo).Set(gen.args[1].CompatiblePass(valueType)) );
			
			return gen.compile(enableDebug);
		} 


		//--------------------------------New object--------------------------------

		public static Func<object[], object>CommonObjectCreator(ConstructorInfo constructorInfo, string name = null, bool enableDebug = false) {
			
			if(name == null) {
				name = constructorInfo.Name + "_Constructor";
			}

			var gen = new ILGen<Func<object[], object>>(name, true);
			Expr[] args = constructorInfo.GetParameters().Select((ParameterInfo parameterInfo, int index) => gen.args[0].Index(Expr.Const(index))).ToArray();
			gen.Return( Expr.CallConstructor(constructorInfo, args) );
			
			return gen.compile(enableDebug);
		} 

		//--------------------------------Methods--------------------------------

		public static Func<object, object[], object> CommonCallShell(MethodInfo methodInfo, string name = null, bool enableDebug = false) {
			if(name == null) {
				name = methodInfo.Name + "_CallShell";
			}
			var gen = new ILGen<Func<object, object[], object>>(name, true);

			//Get args expressions
			Expr[] args = methodInfo.GetParameters().Select((ParameterInfo parameterInfo, int index) => gen.args[1].Index(Expr.Const(index))).ToArray();
			
			if(methodInfo.ReturnType == null || methodInfo.ReturnType == typeof(void)) { //Function doesn't return result
				if(methodInfo.IsStatic) {
					gen.Line( Expr.CallStatic(methodInfo, args) );
				} else {
					gen.Line( gen.args[0].CompatiblePass(methodInfo.DeclaringType).CallMethod(methodInfo, args) );
				}
				gen.Return(Expr.NULL);
			} else {
				if(methodInfo.IsStatic) {
					gen.Return( Expr.CallStatic(methodInfo, args) );
				} else {
					gen.Return( gen.args[0].CompatiblePass(methodInfo.DeclaringType).CallMethod(methodInfo, args) );
				}
			}

			return gen.compile(enableDebug);
		}

		public static Func<object[], object> CommonStaticCallShell(MethodInfo methodInfo, string name = null, bool enableDebug = false) {
			if(name == null) {
				name = methodInfo.Name + "_CallShell";
			}
			var gen = new ILGen<Func<object[], object>>(name, true);

			//Get args expressions
			Expr[] args = methodInfo.GetParameters().Select((ParameterInfo parameterInfo, int index) => gen.args[0].Index(Expr.Const(index))).ToArray();
			
			if(methodInfo.ReturnType == null || methodInfo.ReturnType == typeof(void)) { //Function doesn't return result
				gen.Line( Expr.CallStatic(methodInfo, args) );
				gen.Return(Expr.NULL);
			} else {
				gen.Return( Expr.CallStatic(methodInfo, args) );
			}

			return gen.compile(enableDebug);
		}

		public static Action<object[]> StaticActionCallShell(MethodInfo methodInfo, string name = null, bool enableDebug = false) {
			if(name == null) {
				name = methodInfo.Name + "_CallShell";
			}
			var gen = new ILGen<Action<object[]>>(name, true);

			//Get args expressions
			Expr[] args = methodInfo.GetParameters().Select((ParameterInfo parameterInfo, int index) => gen.args[0].Index(Expr.Const(index))).ToArray();
			gen.Line( Expr.CallStatic(methodInfo, args) );

			return gen.compile(enableDebug);
		}

		public static Func<object[], object> StaticFunctionCallShell(MethodInfo methodInfo, string name = null, bool enableDebug = false) {
			if(name == null) {
				name = methodInfo.Name + "_CallShell";
			}
			var gen = new ILGen<Func<object[], object>>(name, true);

			//Get args expressions
			Expr[] args = methodInfo.GetParameters().Select((ParameterInfo parameterInfo, int index) => gen.args[0].Index(Expr.Const(index))).ToArray();
			gen.Return( Expr.CallStatic(methodInfo, args) );

			return gen.compile(enableDebug);
		}

		public static Func<object, object[], object> CommonInstanceCallShell(MethodInfo methodInfo, string name = null, bool enableDebug = false) {
			if(name == null) {
				name = methodInfo.Name + "_CallShell";
			}
			var gen = new ILGen<Func<object, object[], object>>(name, true);

			//Get args expressions
			Expr[] args = methodInfo.GetParameters().Select((ParameterInfo parameterInfo, int index) => gen.args[1].Index(Expr.Const(index))).ToArray();
			
			if(methodInfo.ReturnType == null || methodInfo.ReturnType == typeof(void)) { //Function doesn't return result
				gen.Line( gen.args[0].CompatiblePass(methodInfo.DeclaringType).CallMethod(methodInfo, args) );
				gen.Return(Expr.NULL);
			} else {
				gen.Return( gen.args[0].CompatiblePass(methodInfo.DeclaringType).CallMethod(methodInfo, args) );
			}

			return gen.compile(enableDebug);
		}

		public static Action<object, object[]> InstanceActionCallShell(MethodInfo methodInfo, string name = null, bool enableDebug = false) {
			if(name == null) {
				name = methodInfo.Name + "_CallShell";
			}
			var gen = new ILGen<Action<object, object[]>>(name, true);

			//Get args expressions
			Expr[] args = methodInfo.GetParameters().Select((ParameterInfo parameterInfo, int index) => gen.args[1].Index(Expr.Const(index))).ToArray();
			gen.Line( gen.args[0].CompatiblePass(methodInfo.DeclaringType).CallMethod(methodInfo, args) );

			return gen.compile(enableDebug);
		}

		public static Func<object, object[], object> InstanceFunctionCallShell(MethodInfo methodInfo, string name = null, bool enableDebug = false) {
			if(name == null) {
				name = methodInfo.Name + "_CallShell";
			}
			var gen = new ILGen<Func<object, object[], object>>(name, true);

			//Get args expressions
			Expr[] args = methodInfo.GetParameters().Select((ParameterInfo parameterInfo, int index) => gen.args[1].Index(Expr.Const(index))).ToArray();
			gen.Return( gen.args[0].CompatiblePass(methodInfo.DeclaringType).CallMethod(methodInfo, args) );

			return gen.compile(enableDebug);
		}
	}
}
