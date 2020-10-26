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


		public static Func<object, object[], object> CommonFunctionCallShell(MethodInfo methodInfo, string name = null) {
			if(name == null) {
				name = methodInfo.Name + "_CallShell";
			}
			var gen = new ILGen<Func<object, object[], object>>(name, true);

			//Get args expressions
			Expr[] args = methodInfo.GetParameters().Select((ParameterInfo parameterInfo, int index) => gen.args[1].Index(Expr.Const(index))).ToArray();
			
			if(methodInfo.ReturnType == null || methodInfo.ReturnType == typeof(void)) { //Function doesn't return result
				if(methodInfo.IsStatic) {
					gen.Line(Expr.CallStatic(methodInfo, args));
				} else {
					gen.Line(gen.args[0].CallMethod(methodInfo, args));
				}
				gen.Return(Expr.NULL);
			} else {
				if(methodInfo.IsStatic) {
					gen.Return(Expr.CallStatic(methodInfo, args));
				} else {
					gen.Return(gen.args[0].CallMethod(methodInfo, args));
				}
			}

			return gen.compile(true);
		}
	}
}
