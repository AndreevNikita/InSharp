using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace LowLevelOpsHelper
{
	public static class NumericOps
	{
		//sbyte - 0, short  - 1, int  - 2, long  - 3,
		//byte  - 4, ushort - 5, uint - 6, ulong - 7,
		//float - 8, double - 9

		public class NativeTypeInfo { 
			public readonly Type Type;
			public readonly int BytesSize;
			public readonly bool IsUnsigned;
			public readonly bool isFloatingPoint;
			public readonly string ShortName;
			public readonly OpCode ConvertionOpCode;
			internal int index = -1;

			internal NativeTypeInfo(Type type, int bytesSize, bool isUnsigned, bool isFloatingPoint, OpCode convertionOpCode, string shortName) { 
				this.Type = type;
				this.BytesSize = bytesSize;
				this.IsUnsigned = isUnsigned;
				this.isFloatingPoint = isFloatingPoint;
				this.ShortName = shortName;
				this.ConvertionOpCode = convertionOpCode;
			}

			public override string ToString() {
				return ShortName;
			}
		}

		public static NativeTypeInfo TypeInfo_I1 = new NativeTypeInfo(typeof(sbyte),	sizeof(sbyte),	false,	false,	OpCodes.Conv_I1,	"i1");
		public static NativeTypeInfo TypeInfo_U1 = new NativeTypeInfo(typeof(byte),	sizeof(byte),	true,	false,	OpCodes.Conv_U1,	"u1");
		public static NativeTypeInfo TypeInfo_I2 = new NativeTypeInfo(typeof(short),	sizeof(short),	false,	false,	OpCodes.Conv_I2,	"i2");
		public static NativeTypeInfo TypeInfo_U2 = new NativeTypeInfo(typeof(ushort),	sizeof(ushort),	true,	false,	OpCodes.Conv_U2,	"u2");
		public static NativeTypeInfo TypeInfo_I4 = new NativeTypeInfo(typeof(int),		sizeof(int),	false,	false,	OpCodes.Conv_I4,	"i4");
		public static NativeTypeInfo TypeInfo_U4 = new NativeTypeInfo(typeof(uint),	sizeof(uint),	true,	false,	OpCodes.Conv_U4,	"u4");
		public static NativeTypeInfo TypeInfo_I8 = new NativeTypeInfo(typeof(long),	sizeof(long),	false,	false,	OpCodes.Conv_I8,	"i8");
		public static NativeTypeInfo TypeInfo_U8 = new NativeTypeInfo(typeof(ulong),	sizeof(ulong),	true,	false,	OpCodes.Conv_U8,	"u8");
		public static NativeTypeInfo TypeInfo_R4 = new NativeTypeInfo(typeof(float),	sizeof(float),	false,	true,	OpCodes.Conv_R4,	"r4");
		public static NativeTypeInfo TypeInfo_R8 = new NativeTypeInfo(typeof(double),	sizeof(double), false,	true,	OpCodes.Conv_R8,	"r8");

		private static NativeTypeInfo[] TypeInfos = {
			TypeInfo_I1,
			TypeInfo_U1,
			TypeInfo_I2,
			TypeInfo_U2,
			TypeInfo_I4,
			TypeInfo_U4,
			TypeInfo_I8,
			TypeInfo_U8,
			TypeInfo_R4,
			TypeInfo_R8
			
		};

		private static Dictionary<Type, Type> synonyms = new Dictionary<Type, Type>() {
			{ typeof(char), typeof(short) }
		};

		static Type toBaseType(Type type) {
			return synonyms.TryGetValue(type, out Type result) ? result : type;
		}

		public static NativeTypeInfo GetTypeInfoByType(Type type) {
			type = toBaseType(type);
			return Array.Find(TypeInfos, (typeInfo) => typeInfo.Type == type);
		}

		static void swap<T>(ref T a, ref T b) { 
			T buffer = a;
			a = b;
			b = buffer;
		}

		private static NativeTypeInfo[,] opsResultsTypesMatrix = new NativeTypeInfo[TypeInfos.Length, TypeInfos.Length];
		private static NativeTypeInfo[,] opsConvertionsMatrix = new NativeTypeInfo[TypeInfos.Length, TypeInfos.Length];

		//Numeric operations results
		public static NativeTypeInfo GetArithmOpResultType(Type op1Type, Type op2Type) { 
			return GetArithmOpResultType(GetTypeInfoByType(op1Type), GetTypeInfoByType(op2Type));
		}

		public static NativeTypeInfo GetArithmOpResultType(NativeTypeInfo op1TypeInfo, NativeTypeInfo op2TypeInfo) { 
			if(op1TypeInfo == null || op2TypeInfo == null)
				return null;

			return opsResultsTypesMatrix[op1TypeInfo.index, op2TypeInfo.index];
		}

		public static NativeTypeInfo GetArithmOpResultType_Rule(NativeTypeInfo op1TypeInfo, NativeTypeInfo op2TypeInfo) {
			if(op1TypeInfo == null || op2TypeInfo == null)
				return null;

			//Set op2Type domination
			if(op1TypeInfo.IsUnsigned && !op2TypeInfo.IsUnsigned) { 
				swap(ref op1TypeInfo, ref op2TypeInfo);
			}

			if(op1TypeInfo.BytesSize > op2TypeInfo.BytesSize) { 
				swap(ref op1TypeInfo, ref op2TypeInfo);
			}

			if(op1TypeInfo.isFloatingPoint && !op2TypeInfo.isFloatingPoint) {
				swap(ref op1TypeInfo, ref op2TypeInfo);
			}

			//Floating point
			if(op2TypeInfo.isFloatingPoint) { 
				return op2TypeInfo;
			}

			//Int<N> + Int<N>

			//ULong
			if(op2TypeInfo == TypeInfo_U8) {
				if(op1TypeInfo.IsUnsigned) { 
					return TypeInfo_U8;
				} else { 
					return null;
				}
			}

			//Long
			if(op2TypeInfo == TypeInfo_I8) { 
				return TypeInfo_I8;
			}

			//UInt
			if(op2TypeInfo == TypeInfo_U4) { 
				if(op1TypeInfo.IsUnsigned) { 
					return TypeInfo_U4;
				} else { 
					return TypeInfo_I8;
				}
			}

			//Other
			return TypeInfo_I4;
		}

		//Operations inner convertions
		public static NativeTypeInfo GetMSILConvertionType(Type op1Type, Type op2Type) { 
			return GetArithmOpResultType(GetTypeInfoByType(op1Type), GetTypeInfoByType(op2Type));
		}

		public static NativeTypeInfo GetMSILConvertionType(NativeTypeInfo op1TypeInfo, NativeTypeInfo op2TypeInfo) { 
			if(op1TypeInfo == null || op2TypeInfo == null)
				return null;

			return opsConvertionsMatrix[op1TypeInfo.index, op2TypeInfo.index];
		}

		public static NativeTypeInfo GetMSILConvertionType_Rule(NativeTypeInfo baseType, NativeTypeInfo resultType) { 
			if(baseType == null || resultType == null)
				return null;

			if(baseType == resultType)
				return null;

			//Floating points
			if(baseType.isFloatingPoint || resultType.isFloatingPoint)
				return resultType;

			//Integers

			if(baseType.BytesSize == resultType.BytesSize)
				return null;

			//Convert to ULong
			if(resultType.Type == typeof(ulong)) { 
				return baseType.IsUnsigned ? TypeInfo_U8 : TypeInfo_I8;
			}

			//Convert to long
			if(resultType.Type == typeof(long))
				return TypeInfo_I8;

			//Convert i1 - u4
			if(baseType.BytesSize > resultType.BytesSize)
				return resultType;

			return null;
		}

		public static void GetOpConvertionsAndResultTypes(Type op1, Type op2, out NativeTypeInfo op1ConvertionType, out NativeTypeInfo op2ConvertionType, out NativeTypeInfo resultTypeInfo) { 
			resultTypeInfo = GetOpConvertionsAndResultTypes(op1, op2, out op1ConvertionType, out op2ConvertionType);
		}

		public static NativeTypeInfo GetOpConvertionsAndResultTypes(Type op1, Type op2, out NativeTypeInfo op1ConvertionType, out NativeTypeInfo op2ConvertionType) { 
			NativeTypeInfo op1TypeInfo = GetTypeInfoByType(op1);
			NativeTypeInfo op2TypeInfo = GetTypeInfoByType(op2);
			NativeTypeInfo resultTypeInfo = GetArithmOpResultType(op1, op2);
			if(resultTypeInfo != null) { 
				op1ConvertionType = GetMSILConvertionType(op1TypeInfo, resultTypeInfo);
				op2ConvertionType = GetMSILConvertionType(op2TypeInfo, resultTypeInfo);
			} else { 
				op1ConvertionType = null;
				op2ConvertionType = null;
			}
			return resultTypeInfo;
		}

		static NumericOps() {
			for(int index = 0; index < TypeInfos.Length; index++)
				TypeInfos[index].index = index;

			//Cache results in 10x10 matrices
			foreach(NativeTypeInfo op1TypeInfo in TypeInfos) { 
				foreach(NativeTypeInfo op2TypeInfo in TypeInfos) {
					opsResultsTypesMatrix[op1TypeInfo.index, op2TypeInfo.index] = GetArithmOpResultType_Rule(op1TypeInfo, op2TypeInfo);
				}
			}

			foreach(NativeTypeInfo op1TypeInfo in TypeInfos) { 
				foreach(NativeTypeInfo op2TypeInfo in TypeInfos) {
					opsConvertionsMatrix[op1TypeInfo.index, op2TypeInfo.index] = GetMSILConvertionType_Rule(op1TypeInfo, op2TypeInfo);
				}
			}
			
		}

		public static void ShowOpsResultsMatrix() { 
			foreach(NativeTypeInfo op1TypeInfo in TypeInfos) { 
				foreach(NativeTypeInfo op2TypeInfo in TypeInfos) {
					NativeTypeInfo resultType = GetArithmOpResultType(op1TypeInfo, op2TypeInfo);
					Console.Write("{0}; ", resultType != null ? resultType.ToString() : "- ");
				}
				Console.WriteLine();
			}
		}

		public static void ShowMSILConvertionMatrix() { 
			foreach(NativeTypeInfo baseType in TypeInfos) { 
				foreach(NativeTypeInfo resultType in TypeInfos) {
					NativeTypeInfo conventionType = GetMSILConvertionType(baseType, resultType);
					Console.Write("{0}; ", conventionType != null ? conventionType.ToString() : "- ");
				}
				Console.WriteLine();
			}
		}

		public static bool IsNumericOp(Type op1Type, Type op2Type) {
			return IsNumericType(op1Type) && IsNumericType(op2Type);
		}

		public static bool IsNumericType(Type type) { 
			if(!type.IsValueType)
				return false;

			return GetTypeInfoByType(type) != null;
		}

		public static bool IsUnsignedOp(Type op1Type, Type op2Type) { 
			return GetTypeInfoByType(op1Type).IsUnsigned && GetTypeInfoByType(op2Type).IsUnsigned;
		}

		public static NativeTypeInfo GetBitwiseOpResultType(Type op1Type, Type op2Type) { 
			return GetBitwiseOpResultType(GetTypeInfoByType(op1Type), GetTypeInfoByType(op2Type));
		}

		public static NativeTypeInfo GetBitwiseOpResultType(NativeTypeInfo op1TypeInfo, NativeTypeInfo op2TypeInfo) { 
			if(op1TypeInfo == null || op1TypeInfo.isFloatingPoint)
				return null;

			if(op2TypeInfo == null || op2TypeInfo.isFloatingPoint)
				return null;


			return Math.Max(op1TypeInfo.BytesSize, op2TypeInfo.BytesSize) == 8 ? TypeInfo_I8 : TypeInfo_I4;
		}


	}
}
