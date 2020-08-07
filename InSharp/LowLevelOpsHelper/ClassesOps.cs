using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LowLevelOpsHelper {
	public class ClassesOps {

		[Flags]
		public enum CastType { 
			IMPLICIT = 1, EXPLICIT = 2, ALL = IMPLICIT | EXPLICIT
		}

		public static MethodInfo GetBestStaticMethod(Type fromClass, string name, Type[] types) { 
			return fromClass.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, types, null);
		}

		public static MethodInfo FindCastMethod(Type baseType, Type castType, CastType enabledCastTypes = CastType.ALL) {
			MethodInfo methodInfo;
			
			//Search in target class <TargetClass>(<BaseClass> arg)
			if((enabledCastTypes & CastType.IMPLICIT) != 0) {
				methodInfo = castType.GetMethod("op_Implicit", new[] { baseType }); 
				if(methodInfo != null)
					return methodInfo;

				methodInfo = GetBestStaticMethod(baseType, "op_Implicit", new[] { baseType }); 
				if(methodInfo != null)
					return methodInfo;
			}
			if((enabledCastTypes & CastType.EXPLICIT) != 0) {
				methodInfo = castType.GetMethod("op_Explicit", new[] { baseType }); 
				if(methodInfo != null)
					return methodInfo;

				methodInfo = GetBestStaticMethod(baseType, "op_Explicit", new[] { baseType }); 
				if(methodInfo != null)
					return methodInfo;
			}
			return null;
		}

		public abstract class ClassesOperation { 
			public string MethodName { get; }

			internal ClassesOperation(string methodName) { 
				this.MethodName = methodName;
			}
		}

		public class BinaryOperation : ClassesOperation {
			internal BinaryOperation(string methodName) : base(methodName) {}

			public MethodInfo FindMethod(Type op1Type, Type op2Type) {
				if(this == OPERATION_ADD) {
					if(op1Type == typeof(string) || op2Type == typeof(string))
						return typeof(string).GetMethod("Concat", new[] { typeof(object), typeof(object) });
				}

				return ClassesOps.FindMethod(op1Type, op2Type, this);
			} 

			public MethodInfo GetMethod(Type fromClass, Type op1Type, Type op2Type) {
				return GetBestStaticMethod(fromClass, MethodName, new[] { op1Type, op2Type });
			}

			public bool isNativeOp(Type op1Type, Type op2Type) {
				return NumericOps.GetArithmOpResultType(op1Type, op2Type) != null;
			}
		}

		public class UnaryOperation : ClassesOperation {
			internal UnaryOperation(string methodName) : base(methodName) {}

			public MethodInfo GetMethod(Type fromClass, Type op1Type) {
				return GetBestStaticMethod(fromClass, MethodName, new[] { op1Type });
			}

			public MethodInfo FindMethod(Type op1Type) { 
				return ClassesOps.FindMethod(op1Type, this);
			} 

			
		}

		public static UnaryOperation OPERATION_UN_PLUS = new UnaryOperation("op_UnaryPlus");
		public static UnaryOperation OPERATION_UN_MINUS = new UnaryOperation("op_UnaryNegation");
		public static UnaryOperation OPERATION_INC = new UnaryOperation("op_Increment");
		public static UnaryOperation OPERATION_DEC = new UnaryOperation("op_Decrement");
		public static UnaryOperation OPERATION_NOT = new UnaryOperation("op_LogicalNot");
		public static UnaryOperation OPERATION_BIT_INVERSE = new UnaryOperation("op_OnesComplement");
		public static UnaryOperation OPERATION_TRUE = new UnaryOperation("op_True" );
		public static UnaryOperation OPERATION_FALSE = new UnaryOperation("op_False");

		public static BinaryOperation OPERATION_ADD = new BinaryOperation("op_Addition");
		public static BinaryOperation OPERATION_SUB = new BinaryOperation("op_Subtraction");
		public static BinaryOperation OPERATION_MUL = new BinaryOperation("op_Multiply");
		public static BinaryOperation OPERATION_DIV = new BinaryOperation("op_Division");
		public static BinaryOperation OPERATION_BIT_AND = new BinaryOperation("op_BitwiseAnd");
		public static BinaryOperation OPERATION_BIT_OR = new BinaryOperation("op_BitwiseOr");
		public static BinaryOperation OPERATION_EX_OR = new BinaryOperation("op_ExclusiveOr");
		public static BinaryOperation OPERATION_EQUAL = new BinaryOperation("op_Equality");
		public static BinaryOperation OPERATION_NEQUAL = new BinaryOperation("op_Inequality");
		public static BinaryOperation OPERATION_LESS = new BinaryOperation("op_LessThan");
		public static BinaryOperation OPERATION_GREATER = new BinaryOperation("op_GreaterThan");
		public static BinaryOperation OPERATION_LEQUAL = new BinaryOperation("op_LessThanOrEqual");
		public static BinaryOperation OPERATION_GEQUAL = new BinaryOperation("op_GreaterThanOrEqual");
		public static BinaryOperation OPERATION_LEFT_SHIFT = new BinaryOperation("op_LeftShift");
		public static BinaryOperation OPERATION_RIGHT_SHIFT = new BinaryOperation("op_RightShift");
		public static BinaryOperation OPERATION_MOD = new BinaryOperation("op_Modulus");
		
		//Classes distance
		public static int GetDistanceTo(Type child, Type parent) { 
			if(!parent.IsAssignableFrom(child)) { 
				return -1;
			}

			int counter = 0;
			for(Type currentType = child; currentType != parent; currentType = currentType.BaseType) { 
				counter++;
			}
			return counter;
		}

		//For searching for the best method with passed args
		public static int GetMethodParamsDistance(MethodInfo methodInfo, params Type[] targetArgs) {
			int result = -1;
			ParameterInfo[] methodParams = methodInfo.GetParameters();
			if(methodParams.Length != targetArgs.Length)
				return -1;
			for(int index = 0; index < targetArgs.Length; index++) { 
				int argDistance = GetDistanceTo(targetArgs[index], methodParams[index].ParameterType);
				if(argDistance == -1)
					return -1;
				result += argDistance;
			}
			return result;
		}

		public static MethodInfo FindMethod(Type op1Type, Type op2Type, BinaryOperation classesOp) { 
			
			//Search in op1 class
			MethodInfo op1TreeMethod = classesOp.GetMethod(op1Type, op1Type, op2Type);

			if(op1Type == op2Type)
				return op1TreeMethod;

			//Search in op2 class
			MethodInfo op2TreeMethod = classesOp.GetMethod(op2Type, op1Type, op2Type);

			if(op1TreeMethod == op2TreeMethod)
				return op1TreeMethod;

			if(op1TreeMethod != null && op2TreeMethod != null) { 
				Type[] typesArray = new[] { op1Type, op2Type };

				int distance1 = GetMethodParamsDistance(op1TreeMethod, typesArray);
				int distance2 = GetMethodParamsDistance(op2TreeMethod, typesArray);

				if(distance1 == distance2)
					return null;

				return distance1 < distance2 ? op1TreeMethod : op2TreeMethod;
			} else if(op1TreeMethod != null) { 
				return op1TreeMethod;
			} else {
				return op2TreeMethod;
			}
			
		}

		public static MethodInfo FindMethod(Type op1Type, UnaryOperation operation) { 
			return op1Type.GetMethod(operation.MethodName, new[] { op1Type });
		}

		public static void GetMultipleDimentionsArrayGetSet(Type type, out MethodInfo getMethodInfo, out MethodInfo setMethodInfo) {
			getMethodInfo = type.GetMethod("Get");
			setMethodInfo = type.GetMethod("Set");
		}

		public static ConstructorInfo GetMultipleDimentionsArrayConstructor(Type arrayType) {
			Type[] argsTypes = new Type[arrayType.GetArrayRank()];
			for(int index = 0; index < argsTypes.Length; index++)
				argsTypes[index] = typeof(int);
			return arrayType.GetConstructor(argsTypes);
		}

		public static ConstructorInfo GetMultipleDimentionsArrayConstructor(Type elementType, int dimensionsCount) { 
			return GetMultipleDimentionsArrayConstructor(elementType.MakeArrayType(dimensionsCount));
		}

		public static MethodInfo GetArrayLengthMethodInfo(Type type) {
			return type.GetMethod("get_Length");
		}

		public static MethodInfo GetArrayRankMethodInfo(Type type) {
			return type.GetMethod("get_Rank");
		}

		public static MethodInfo GetArrayDimensionLengthMethodInfo(Type type) {
			return type.GetMethod("GetLength");
		}

	}
}
