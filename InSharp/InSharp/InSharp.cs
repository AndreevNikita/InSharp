using LowLevelOpsHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static LowLevelOpsHelper.NumericOps;

/*
 * Примечения:
 * - при вызове виртуального метода
 * - методы всегда вызываются по адресу
 * - метод, не возвращающий значение может быть только корнем дерева выражений
 */

/*
 * Задачи:
 * + создание объектов
 * - создание экземпляров структур
 * - инициализация пустого объекта newobj
 * + многомерные массивы
 * 
 */

namespace InSharp {

	public interface IILEmitable {
		void emit(ILGen gen);
	}

	public abstract class Expr {

		public abstract Type Type { get; }
		public bool IsVoidOrNullType { get => Type == null || Type == typeof(void); }

		public Expr NULL { get => new ILNull(); }

		public abstract void emitPush(ILGen gen);

		public void emitPush(ILGen gen, Type targetType) { 
			if(ILCast.MustBeCasted(Type, targetType)) {
				new ILCast(this, targetType).emitPush(gen);
				return;
			}
			emitPush(gen);

		}

		public void assetNullType(string text = "Get member from none type") { 
			if(IsVoidOrNullType)
				throw new InSharpException(text);
		}

		public void assetCheckArrayType(string text = "Expression isn't array") { 
			if(!Type.IsArray)
				throw new InSharpException(text);
		}

		/*--------------------------------Method--------------------------------*/

		public ILMethodCall CallMethod(MethodInfo methodInfo, params Expr[] args) { 
			assetNullType();
			return new ILMethodCall(this, methodInfo, args);
		}

		public ILMethodCall CallMethod(string methodName, params Expr[] args) { 
			assetNullType();
			MethodInfo methodInfo = Type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args.Select((Expr expression) => { 
				expression.assetNullType("None type operand");
				return expression.Type; 
			}).ToArray(), null);
			return CallMethod(methodInfo, args);
		}

		public static ILMethodCall CallStatic(MethodInfo methodInfo, params Expr[] args) {
			return new ILMethodCall(null, methodInfo, args);
		}

		public static ILMethodCall CallStatic(Type classType, string methodName, params Expr[] args) {
			MethodInfo methodInfo = classType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, args.Select((Expr expression) => { 
				expression.assetNullType("None type operand");
				return expression.Type; 
			}).ToArray(), null);
			return CallStatic(methodInfo, args);
		}

		/*--------------------------------Field--------------------------------*/

		public ILField Field(FieldInfo fieldInfo) { 
			return new ILField(this, fieldInfo);
		} 

		public ILField Field(string name) { 
			FieldInfo fieldInfo = Type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if(fieldInfo == null)
				throw new InSharpException("Field \"{0}\" not found", name);
			return Field(fieldInfo);
		}

		public static ILField StaticField(FieldInfo fieldInfo) { 
			return new ILField(null, fieldInfo);
		} 

		public static ILField StaticField(Type classType, string name) { 
			FieldInfo fieldInfo = classType.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if(fieldInfo == null)
				throw new InSharpException("Field \"{0}\" not found", name);
			return StaticField(fieldInfo);
		}

		/*--------------------------------Property--------------------------------*/

		public ILProperty Property(PropertyInfo propertyInfo) { 
			return new ILProperty(this, propertyInfo);
		}

		public ILProperty Property(string name) { 
			PropertyInfo propertyInfo = Type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if(propertyInfo == null)
				throw new InSharpException("Property \"{0}\" not found", name);
			return Property(propertyInfo);
		}

		public static ILProperty StaticProperty(PropertyInfo propertyInfo) { 
			return new ILProperty(null, propertyInfo);
		}

		public static ILProperty StaticProperty(Type classType, string name) { 
			PropertyInfo propertyInfo = classType.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if(propertyInfo == null)
				throw new InSharpException("Property \"{0}\" not found", name);
			return StaticProperty(propertyInfo);
		}

		/*--------------------------------All members--------------------------------*/

		public ExprMember Member(string name) {
			MemberInfo[] members = Type.GetMember(name, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if(members.Length == 0)
				return new ExprMember(this, name);
			if(members.Length != 1)
				throw new InSharpException("Multiple members were found");

			return new ExprMember(this, members[0]);
		}

		public ExprMember Member(MemberInfo memberInfo) { 
			return new ExprMember(this, memberInfo);
		}

		public static ExprMember StaticMember(MemberInfo memberInfo) { 
			return new ExprMember(memberInfo);
		}

		public static ExprMember StaticMember(Type type, string name) { 
			MemberInfo[] members = type.GetMember(name, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if(members.Length == 0)
				return new ExprMember(type, name);
			if(members.Length != 1)
				throw new InSharpException("Multiple members were found");

			return StaticMember(members[0]);
		}


		public static implicit operator Expr(sbyte value) { return Const(value); } 
		public static implicit operator Expr(short value) { return Const(value); }
		public static implicit operator Expr(int value) { return Const(value); }
		public static implicit operator Expr(long value) { return Const(value); }

		public static implicit operator Expr(byte value) { return Const(value); }
		public static implicit operator Expr(ushort value) { return Const(value); }
		public static implicit operator Expr(uint value) { return Const(value); }
		public static implicit operator Expr(ulong value) { return Const(value); }

		public static implicit operator Expr(float value) { return Const(value); }
		public static implicit operator Expr(double value) { return Const(value); }

		public static implicit operator Expr(string value) { return Const(value); }

		public static ILConst Const(object value) { 
			return new ILConst(value);
		}

		public Expr CompatiblePass(Type targetType) { 
			if(ILCast.MustBeCasted(Type, targetType))
				return new ILCast(this, targetType);
			else
				return this;
		}

		public override string ToString() {
			return String.Format("Expression ({0})", Type);
		}

		public ILVar BufferTempVar { get; private set; } = null;
		public bool IsBuffered { get => BufferTempVar != null; }

		public ILVar AllocTemp(ILGen gen) { 
			if(IsBuffered)
				throw new InSharpException("[Inner exception] Variable already buffered");

			return BufferTempVar = gen.GetTemporaryVariable(Type);
		}

		public ILVar EmitSaveToTemp(ILGen gen) {
			ILVar tempVar = AllocTemp(gen);

			tempVar.emitPop(gen);
			return tempVar;
		}

		public void FreeTemp(ILGen gen) {
			gen.FreeTemporaryVar(Type);
			BufferTempVar = null;
		}

		public static Expr operator+(Expr expr1, Expr expr2) {  return Ops.Add.getExpression(expr1, expr2); }
		public static Expr Add(Expr expr1, Expr expr2) {  return Ops.Add.getExpression(expr1, expr2); }
		public static Expr operator-(Expr expr1, Expr expr2) {  return Ops.Sub.getExpression(expr1, expr2); }
		public static Expr Sub(Expr expr1, Expr expr2) {  return Ops.Sub.getExpression(expr1, expr2); }
		public static Expr operator*(Expr expr1, Expr expr2) {  return Ops.Mul.getExpression(expr1, expr2); }
		public static Expr Mul(Expr expr1, Expr expr2) {  return Ops.Mul.getExpression(expr1, expr2); }
		public static Expr operator/(Expr expr1, Expr expr2) {  return Ops.Div.getExpression(expr1, expr2); }
		public static Expr Div(Expr expr1, Expr expr2) {  return Ops.Div.getExpression(expr1, expr2); }
		public static Expr operator%(Expr expr1, Expr expr2) {  return Ops.Mod.getExpression(expr1, expr2); }
		public static Expr Mod(Expr expr1, Expr expr2) {  return Ops.Mod.getExpression(expr1, expr2); }

		public static Expr operator&(Expr expr1, Expr expr2) {  return Ops.And.getExpression(expr1, expr2); }
		public static Expr And(Expr expr1, Expr expr2) {  return Ops.And.getExpression(expr1, expr2); }
		public static Expr operator|(Expr expr1, Expr expr2) {  return Ops.Or.getExpression(expr1, expr2); }
		public static Expr Or(Expr expr1, Expr expr2) {  return Ops.Or.getExpression(expr1, expr2); }
		public static Expr operator^(Expr expr1, Expr expr2) {  return Ops.XOr.getExpression(expr1, expr2); }
		public static Expr XOr(Expr expr1, Expr expr2) {  return Ops.XOr.getExpression(expr1, expr2); }
		public static Expr RightShift(Expr expr1, Expr expr2) {  return Ops.RightShift.getExpression(expr1, expr2); }
		public static Expr LeftShift(Expr expr1, Expr expr2) {  return Ops.LeftShift.getExpression(expr1, expr2); }

		public static Expr Equals(Expr expr1, Expr expr2) {  return Ops.Equal.getExpression(expr1, expr2); }
		public static Expr operator<(Expr expr1, Expr expr2) {  return Ops.Less.getExpression(expr1, expr2); }
		public static Expr Less(Expr expr1, Expr expr2) {  return Ops.Less.getExpression(expr1, expr2); }
		public static Expr operator>(Expr expr1, Expr expr2) {  return Ops.Greater.getExpression(expr1, expr2); }
		public static Expr Greater(Expr expr1, Expr expr2) {  return Ops.Greater.getExpression(expr1, expr2); }
		public static Expr NotEquals(Expr expr1, Expr expr2) {  return Ops.NEqual.getExpression(expr1, expr2); }
		public static Expr operator>=(Expr expr1, Expr expr2) {  return Ops.GEqual.getExpression(expr1, expr2); }
		public static Expr GEquals(Expr expr1, Expr expr2) {  return Ops.GEqual.getExpression(expr1, expr2); }
		public static Expr operator<=(Expr expr1, Expr expr2) {  return Ops.LEqual.getExpression(expr1, expr2); }
		public static Expr LEquals(Expr expr1, Expr expr2) {  return Ops.LEqual.getExpression(expr1, expr2); }


		public static ILSetExpression Set(ILAssignable target, Expr value) { 
			return new ILSetExpression(target, value.CompatiblePass(target.Type));
		}

		//Constructors
		public static Expr NewObject(ConstructorInfo constructor, params Expr[] args) { 
			return CallConstructor(constructor, args);
		}

		public static Expr CallConstructor(ConstructorInfo constructor, params Expr[] args) { 
			return new ILConstructorCall(constructor, args);
		}

		public static Expr NewObject(Type classType, params Expr[] args) { 
			return CallConstructor(classType, args);
		}

		public static Expr NewObjectDefault(Type classType) { 
			return new ILConstructorCall(classType);
		}


		public static Expr CallConstructor(Type classType, params Expr[] args) {
			ConstructorInfo constructorInfo = classType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args.Select((Expr expression) => { 
				expression.assetNullType("None type operand");
				return expression.Type; 
			}).ToArray(), null);

			Console.WriteLine("CTorInfo: {0}", constructorInfo);

			if(constructorInfo == null) { 
				if(args.Length != 0)
					throw new InSharpException("Constructor not found");

				return NewObjectDefault(classType);
			}

			return CallConstructor(constructorInfo, args);
		}

		//Arrays

		public static Expr NewArray(Type elementType, params Expr[] size) {
			if(size.Length < 1)
				throw new InSharpException("Array can't have {0} dimensions", size.Length);

			if(size.Length == 1)
				return new ILCreateArray(elementType, size[0]);
			else
				return CallConstructor(ClassesOps.GetMultipleDimentionsArrayConstructor(elementType.MakeArrayType(size.Length)), size);
		}

		public ILAssignable Index(params Expr[] indexExpressions) {
			assetNullType();
			assetCheckArrayType();

			int arrayRank = Type.GetArrayRank();
			if(arrayRank != indexExpressions.Length)
					throw new InSharpException("Index args count ({0}) isn't equals array rank ({1})", indexExpressions.Length);
			if(arrayRank == 1)
				return new ILArrayIndex(this, indexExpressions[0]);
			else {
				ClassesOps.GetMultipleDimentionsArrayGetSet(Type, out MethodInfo getMethodInfo, out MethodInfo setMethodInfo);
				return new ILMDArrayIndex(this, getMethodInfo, setMethodInfo, indexExpressions);//new ILProperty(this, );
			}
		}

		public Expr ArrayLength { 
			get { 
				assetNullType();
				assetCheckArrayType();

				return new ILArrayLength(this);
			} 
		}

		public Expr GetArrayDimensionLength(Expr dimension) {
			return new ILMethodCall(this, ClassesOps.GetArrayDimensionLengthMethodInfo(Type));
		}

		public Expr ArrayRank { 
			get { 
				assetNullType();
				assetCheckArrayType();

				return new ILMethodCall(this, ClassesOps.GetArrayRankMethodInfo(Type));
			}
		}

	}

	public class ExprMember { 
		Expr ownerExpression = null;
		MemberInfo memberInfo;
		string memberName = null;
		Type ownerType = null;

		public ExprMember(Expr ownerExpression, MemberInfo memberInfo) : this(memberInfo) { 
			this.ownerExpression = ownerExpression;
		}

		//For static members

		public ExprMember(MemberInfo memberInfo) { 
			this.memberInfo = memberInfo;
		}
		
		//Only for lazy methods search
		public ExprMember(Expr ownerExpression, string memberName) { 
			this.ownerExpression = ownerExpression;
			this.memberName = memberName;
		}

		public ExprMember(Type ownerType, string memberName) { 
			this.ownerType = ownerType;
			this.memberName = memberName;
		}


		public ILMethodCall call(params Expr[] args) {
			if(ownerExpression.IsNull()) {
				return memberInfo != null ? Expr.CallStatic((MethodInfo)memberInfo, args) : Expr.CallStatic(ownerType, memberName, args);
			}  else { 
				return memberInfo != null ? ownerExpression.CallMethod((MethodInfo)memberInfo, args) : ownerExpression.CallMethod(memberName, args);
			}	
		}

		public static implicit operator ILAssignable(ExprMember exprMember) { 
			switch (exprMember.memberInfo.MemberType) {
				case MemberTypes.Field:
					return exprMember.ownerExpression.IsNull() ? Expr.StaticField((FieldInfo)exprMember.memberInfo) : exprMember.ownerExpression.Field((FieldInfo)exprMember.memberInfo);
				case MemberTypes.Property:
					return exprMember.ownerExpression.IsNull() ? Expr.StaticProperty((PropertyInfo)exprMember.memberInfo) : exprMember.ownerExpression.Property((PropertyInfo)exprMember.memberInfo);
				default:
					throw new InSharpException("Unknown memver type");
			}
		}

		public ILSetExpression Set(Expr value) {
			return ((ILAssignable)this).Set(value);
		}

	}

	public static class ExprExtend { 
		public static bool IsNull(this Expr expr) {
			return object.ReferenceEquals(expr, null);
		}

	}

	public abstract class ILAssignable : Expr { 
		public virtual void emitPushAddress(ILGen gen) { }

		public virtual void emitPrepop(ILGen gen) { }

		public abstract void emitPop(ILGen gen);

		public ILSetExpression Set(Expr value) {
			return Expr.Set(this, value);
		}

	}


	public class ILArg : ILAssignable {
		public override Type Type { get; }
		private int index;

		public ILArg(int index, Type type) { 
			this.index = index;
			Type = type;
		}

		public override void emitPushAddress(ILGen gen) {
			gen.il.Emit(OpCodes.Ldarga_S, index);
			gen.OutDebug("OpCodes.Ldarga_S, {0}", index);
		}

		public override void emitPush(ILGen gen) {
			gen.il.Emit(OpCodes.Ldarg_S, index);
			gen.OutDebug("OpCodes.Ldarg_S, {0}", index);
		}

		public override void emitPop(ILGen gen) { 
			gen.il.Emit(OpCodes.Starg_S, index);
			gen.OutDebug("OpCodes.Starg_S, {0}", index);
		}
	}

	public class ILVar : ILAssignable { 

		protected LocalBuilder localVariable;

		public override Type Type { get => localVariable.LocalType; }

		public ILVar(LocalBuilder localVariable) { 
			this.localVariable = localVariable;
		}

		public override void emitPushAddress(ILGen gen) {
			gen.il.Emit(OpCodes.Ldloca, localVariable);
			gen.OutDebug("OpCodes.Ldloca, {0}", localVariable);
		}

		public override void emitPush(ILGen gen) {
			gen.il.Emit(OpCodes.Ldloc, localVariable);
			gen.OutDebug("OpCodes.Ldloc, {0}", localVariable);
		}

		public override void emitPop(ILGen gen) { 
			gen.il.Emit(OpCodes.Stloc, localVariable);
			gen.OutDebug("OpCodes.Stloc, {0}", localVariable);
		}
	}

	public class ILField : ILAssignable {
		
		public override Type Type { get => fieldInfo.FieldType; }

		private Expr ownerExpression;
		private FieldInfo fieldInfo;

		public ILField(Expr ownerExpression, FieldInfo fieldInfo) { 
			this.ownerExpression = ownerExpression;
			this.fieldInfo = fieldInfo;
		}

		public override void emitPushAddress(ILGen gen) {
			ownerExpression.emitPush(gen);
			gen.il.Emit(OpCodes.Ldflda, fieldInfo);
			gen.OutDebug("OpCodes.Ldflda {0}", fieldInfo);
		}

		public override void emitPush(ILGen gen) {
			ownerExpression.emitPush(gen);
			gen.il.Emit(OpCodes.Ldfld, fieldInfo);
			gen.OutDebug("OpCodes.Ldfld {0}", fieldInfo);
		}

		public override void emitPrepop(ILGen gen) { 
			if(ownerExpression.Type.IsValueType) { 
				((ILAssignable)ownerExpression).emitPushAddress(gen);
			} else { 
				ownerExpression.emitPush(gen);
			}
		}

		public override void emitPop(ILGen gen) {
			gen.il.Emit(OpCodes.Stfld, fieldInfo);
			gen.OutDebug("OpCodes.Stfld, {0}", fieldInfo);
		}
		
	}

	public class ILProperty : ILAssignable { 
		public override Type Type { get; } 

		public readonly Expr ownerExpression;
		public readonly PropertyInfo property;
		public readonly MethodInfo getMethod;
		public readonly MethodInfo setMethod;

		public ILProperty(Expr ownerExpression, PropertyInfo property) { 
			this.property = property;
			this.getMethod = property.GetMethod;
			this.setMethod = property.SetMethod;
			this.ownerExpression = ownerExpression;
			this.Type = property.PropertyType;
		}

		public override void emitPrepop(ILGen gen) {
			if(!ownerExpression.IsNull()) {
				if(ownerExpression.Type.IsValueType) { 
					if(ownerExpression is ILAssignable)
						((ILAssignable)ownerExpression).emitPushAddress(gen);
					else
						throw new InSharpException("Field owner is nor variable!");
				} else { 
					ownerExpression.emitPush(gen);
				}
				
			}

		}

		public override void emitPush(ILGen gen) {
			new ILMethodCall(ownerExpression, getMethod).emitPush(gen);
		}

		public override void emitPop(ILGen gen) {
			if(ownerExpression.IsNull() || ownerExpression.Type.IsValueType) {
				gen.il.Emit(OpCodes.Call, setMethod);
				gen.OutDebug("OpCodes.Call {0}", setMethod);
			} else { 
				gen.il.Emit(OpCodes.Callvirt, setMethod);
				gen.OutDebug("OpCodes.Callvirt {0}", setMethod);
			}
		}
	}

	public class ILMDArrayIndex : ILAssignable { 
		public override Type Type { get; } 

		public readonly Expr ownerExpression;
		public readonly MethodInfo getMethod;
		public readonly MethodInfo setMethod;
		private Expr[] args; 

		public ILMDArrayIndex(Expr ownerExpression, MethodInfo getMethod, MethodInfo setMethod, params Expr[] passedArgs) { 
			this.getMethod = getMethod;
			this.setMethod = setMethod;
			this.ownerExpression = ownerExpression;
			this.Type = ownerExpression.Type.GetElementType();
			this.args = passedArgs.Select((arg) => arg.Type == typeof(int) ? arg : arg.CompatiblePass(typeof(int))).ToArray();
		}

		public override void emitPrepop(ILGen gen) {
			ownerExpression.emitPush(gen);
			foreach(Expr arg in args)
				arg.emitPush(gen);
		}

		public override void emitPush(ILGen gen) {
			ownerExpression.emitPush(gen);
			foreach(Expr arg in args)
				arg.emitPush(gen);
			gen.il.Emit(OpCodes.Call, getMethod);
			gen.OutDebug("OpCodes.Call {0}", getMethod);
		}

		public override void emitPop(ILGen gen) {
			gen.il.Emit(OpCodes.Call, setMethod);
			gen.OutDebug("OpCodes.Call {0}", setMethod);
		}
	}

	public class ILSetExpression : Expr, IILEmitable { 
		ILAssignable target;
		Expr valueExpression;

		public override Type Type { get; }

		public ILSetExpression(ILAssignable target, Expr valueExpression) { 
			this.target = target;
			this.valueExpression = valueExpression.CompatiblePass(target.Type);
		}

		public void emit(ILGen gen) {
			target.emitPrepop(gen);
			valueExpression.emitPush(gen);
			target.emitPop(gen);
		}

		public override void emitPush(ILGen gen) {
			target.emitPrepop(gen);
			valueExpression.emitPush(gen);
			gen.il.Emit(OpCodes.Dup);
			gen.OutDebug("OpCodes.Dup");
			ILVar tempVar = gen.GetTemporaryVariable(valueExpression.Type);
			tempVar.emitPop(gen);
			target.emitPop(gen);
			tempVar.emitPush(gen);
		}

	}

	public class ILConst : Expr {
		public override Type Type { get; }
		private object value;

		public ILConst(object value) { 
			Type = value.GetType();
			this.value = value;
		}

		public override void emitPush(ILGen gen) {
			if(Type == typeof(byte) || Type == typeof(sbyte) ||
				Type == typeof(ushort) || Type == typeof(short) || 
				Type == typeof(uint) || Type == typeof(int)) { 
				PushOptimizedInt(value, gen);
			} else if(Type == typeof(long) || Type == typeof(ulong)) { 
				gen.il.Emit(OpCodes.Ldc_I8, Convert.ToInt64(value));
				gen.OutDebug("OpCodes.Ldc_I8, {0}", value);
			} else if(Type == typeof(bool)) { 
				gen.il.Emit(Convert.ToBoolean(value) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
				gen.OutDebug(Convert.ToBoolean(value) ? "OpCodes.Ldc_I4_1" : "OpCodes.Ldc_I4_0");
			} else if(Type == typeof(float)) {
				gen.il.Emit(OpCodes.Ldc_R4, Convert.ToSingle(value));
				gen.OutDebug("OpCodes.Ldc_R4, {0}", value);
			} else if(Type == typeof(double)) {
				gen.il.Emit(OpCodes.Ldc_R8, Convert.ToDouble(value));
				gen.OutDebug("OpCodes.Ldc_R8, {0}", value);
			} else if(Type == typeof(string)) { 
				gen.il.Emit(OpCodes.Ldstr, Convert.ToString(value));
				gen.OutDebug("OpCodes.Ldstr, \"{0}\"", value);
			}
		}

		public static OpCode? GetOptimizeIntOpcode(int value) {
			switch(value) {
				case 0:
					return OpCodes.Ldc_I4_0;
				case 1:
					return OpCodes.Ldc_I4_1;
				case 2:
					return OpCodes.Ldc_I4_2;
				case 3:
					return OpCodes.Ldc_I4_3;
				case 4:
					return OpCodes.Ldc_I4_4;
				case 5:
					return OpCodes.Ldc_I4_5;
				case 6:
					return OpCodes.Ldc_I4_6;
				case 7:
					return OpCodes.Ldc_I4_7;
				case 8:
					return OpCodes.Ldc_I4_8;
			}
			return null;
		}

		public static void PushOptimizedInt(object value, ILGen gen) {
			OpCode? optimizedOpCode = GetOptimizeIntOpcode((int)value);
			if(optimizedOpCode != null) { 
				gen.il.Emit(optimizedOpCode.Value);
				gen.OutDebug("OpCodes.Ldc_I4_{0}", value);
			} else { 
				gen.il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(value));
				gen.OutDebug("OpCodes.Ldc_I4, {0}", value);
			}
		}
	}

	public class ILNull : Expr {
		public override Type Type => typeof(object);

		public override void emitPush(ILGen gen) {
			gen.il.Emit(OpCodes.Ldnull);
			gen.OutDebug("OpCodes.Ldnull");
		}
	}

	public class ILCreateArray : Expr { 
		Type elementType;

		public override Type Type { get; } 

		Expr sizeExpr;

		public ILCreateArray(Type elementType, Expr sizeExpr) { 
			this.elementType = elementType;
			this.Type = elementType.MakeArrayType();
			this.sizeExpr = sizeExpr;
		}

		public override void emitPush(ILGen gen) {
			sizeExpr.emitPush(gen);
			gen.il.Emit(OpCodes.Newarr, elementType);
			gen.OutDebug("OpCodes.Newarr, {0}", elementType);
		}
	}



	public class ILArrayIndex : ILAssignable { 
		public override Type Type { get => ownerExpression.Type.GetElementType(); } 

		public readonly Expr ownerExpression;
		public readonly Expr indexExpression;

		public ILArrayIndex(Expr ownerExpression, Expr indexExpression) { 
			this.ownerExpression = ownerExpression;
			this.indexExpression = indexExpression;
		}

		public override void emitPrepop(ILGen gen) { 
			ownerExpression.emitPush(gen);
			indexExpression.emitPush(gen);
		}

		public override void emitPop(ILGen gen) { 
			if(Type.IsValueType) { 
				gen.il.Emit(OpCodes.Stelem, Type);
				gen.OutDebug("OpCodes.Stelem, {0}", Type);
			} else { 
				gen.il.Emit(OpCodes.Stelem_Ref);
				gen.OutDebug("OpCodes.Stelem_Ref");
			}
		}

		public override void emitPush(ILGen gen) {
			ownerExpression.emitPush(gen);
			indexExpression.emitPush(gen);

			if(Type.IsValueType) { 
				gen.il.Emit(OpCodes.Ldelem, Type);
				gen.OutDebug("OpCodes.Ldelem, {0}", Type);
			} else { 
				gen.il.Emit(OpCodes.Ldelem_Ref);
				gen.OutDebug("OpCodes.Ldelem_Ref");
			}
		}
	}

	public class ILArrayLength : Expr {
		public override Type Type { get => typeof(int); } 
		public readonly Expr ownerExpression;

		public ILArrayLength(Expr ownerExpression) { 
			this.ownerExpression = ownerExpression;
		}

		public override void emitPush(ILGen gen) {
			if(ownerExpression.Type.GetArrayRank() == 1) { //1D array length
				ownerExpression.emitPush(gen);
				gen.il.Emit(OpCodes.Ldlen);
				gen.OutDebug("OpCodes.Ldlen");
				gen.il.Emit(OpCodes.Conv_I4);
				gen.OutDebug("OpCodes.Conv_I4");
			} else { 
				new ILMethodCall(ownerExpression, ClassesOps.GetArrayLengthMethodInfo(ownerExpression.Type)).emitPush(gen);
			}
			
		}
	}

	public abstract class ILArgsExpression : Expr, IILEmitable { 

		protected Expr[] args;

		public ILArgsExpression(ParameterInfo[] parameters, Expr[] passedArgs) { 
			this.args = new Expr[parameters.Length];

			for(int index = 0; index < parameters.Length; index++) {
				ParameterInfo parameterInfo = parameters[index];
				if(index == parameters.Length - 1) {
					if(parameterInfo.GetCustomAttribute(typeof(ParamArrayAttribute), false) != null) { 
						if(passedArgs.Length == parameters.Length && parameterInfo.ParameterType == passedArgs[index].Type) { 
							args[index] = passedArgs[index];
							break;
						}
							
						Type elementsType = parameterInfo.ParameterType.GetElementType();
						int argsCount = passedArgs.Length - parameters.Length + 1; //Find count of params arguments
						Expr[] arrayElements = new Expr[argsCount];
						for(int argIndex = 0; argIndex < arrayElements.Length; argIndex++) {
							arrayElements[argIndex] = passedArgs[index + argIndex].CompatiblePass(elementsType);
						}

						this.args[index] = new ILCreateArray(elementsType, null/*arrayElements*/);
					} else {
						//Count of arguments isn't variable
						args[index] = passedArgs[index].CompatiblePass(parameterInfo.ParameterType);
					}
				} else { 
					args[index] = passedArgs[index].CompatiblePass(parameterInfo.ParameterType);
				}
				
			}
		}

		//For default constructor without parameters
		public ILArgsExpression() { 
			this.args = null;
		}

		public void emitPushArgs(ILGen gen) { 
			foreach(Expr arg in args) { 
				arg.emitPush(gen);
			}
		}

		protected abstract void emitCall(ILGen gen, bool withResult);

		public override void emitPush(ILGen gen) {
			if(IsVoidOrNullType)
				throw new InSharpException("Expression doesn't returns result");

			emitCall(gen, true);
		}

		public void emit(ILGen gen) {
			emitCall(gen, false);
		}
	}

	public class ILMethodCall : ILArgsExpression {
		public override Type Type { get => methodInfo.ReturnType; }

		protected Expr ownerInstance;
		MethodInfo methodInfo;

		public ILMethodCall(Expr ownerInstance, MethodInfo methodInfo, params Expr[] passedArgs) : base(methodInfo.GetParameters(), passedArgs) { 
			this.ownerInstance = ownerInstance;
			this.methodInfo = methodInfo;
		}

		protected override void emitCall(ILGen gen, bool withResult) { 
			//Если метод не статический, помещаем владельца на вершину стека
			if(!ownerInstance.IsNull()) {
				ownerInstance.assetNullType("Call method from none type");

				//Если владелец метода - структура, в стек нужно поместить ссылку на неё
				if(ownerInstance.Type.IsValueType) {
					if(ownerInstance is ILConstructorCall) { 
						ILConstructorCall constructorCall = (ILConstructorCall)ownerInstance;
						//ONLY STRUCTURE DEFAULT CONSTRUCTOR
						if(constructorCall.IsDefaultConstructor) { 
							constructorCall.emit(gen);
							constructorCall.BufferTempVar.emitPushAddress(gen);
						} else { 
							constructorCall.emitPush(gen); //Помещаем результат работы метода на вершину стека

							//Сохраняем результат во временную переменную
							ILVar tempVar = constructorCall.EmitSaveToTemp(gen);
							tempVar.emitPushAddress(gen);
						}
					} else if(ownerInstance is ILMethodCall) { //Если метод вызывается у результата работы другого метода
						

						ownerInstance.emitPush(gen); //Помещаем результат работы метода на вершину стека

						//Сохраняем результат во временную переменную
						ILVar tempVar = ownerInstance.EmitSaveToTemp(gen);
						tempVar.emitPushAddress(gen);
					} else if(ownerInstance is ILVar) { //Если метод вызывается у переменной, добавляем на вершину стека её адрес
						((ILVar)ownerInstance).emitPushAddress(gen);
					}
				} else { 
					//Добавляем на вершину стека адрес владельца метода
					ownerInstance.emitPush(gen);
				}
			}

			//Помещаем аргументы в стек
			emitPushArgs(gen);

			//Вызов метода
			if(ownerInstance.IsNull() || ownerInstance.Type.IsValueType) { 
				gen.il.Emit(OpCodes.Call, methodInfo);
				gen.OutDebug("OpCodes.Call, {0}", methodInfo);
			} else { 
				gen.il.Emit(OpCodes.Callvirt, methodInfo);
				gen.OutDebug("OpCodes.Callvirt, {0}", methodInfo);
			}

			//Если метод возвращает результат, а он не нужен, делаем pop
			if(!IsVoidOrNullType && (!withResult)) {
				gen.Pop();
			}

			if(!ownerInstance.IsNull() && ownerInstance.IsBuffered)
				ownerInstance.FreeTemp(gen);
		}

		
	}

	public class ILConstructorCall : ILArgsExpression { 

		public override Type Type { get; } 

		ConstructorInfo constructorInfo;
		public bool IsDefaultConstructor { get => constructorInfo == null; }

		public ILConstructorCall(ConstructorInfo constructorInfo, Expr[] passedArgs) : base(constructorInfo.GetParameters(), passedArgs) { 
			this.constructorInfo = constructorInfo;
			this.Type = constructorInfo.DeclaringType;
		}

		public ILConstructorCall(Type type) : base() { 
			this.constructorInfo = null;
			this.Type = type;
		}


		protected override void emitCall(ILGen gen, bool withResult) {
			gen.OutComment("//Call constructor");

			if(IsDefaultConstructor) { 
				ILVar tempVAR = AllocTemp(gen);
				tempVAR.emitPushAddress(gen);
				gen.il.Emit(OpCodes.Initobj, Type);
				gen.OutDebug("OpCodes.Initobj {0}", Type);
				if(withResult)
					tempVAR.emitPush(gen);
				return;
			}

			emitPushArgs(gen);

			if(!Type.IsValueType) {
				gen.il.Emit(OpCodes.Newobj, constructorInfo);
				gen.OutDebug("OpCodes.NewObj {0}", constructorInfo);
			} else {
				gen.il.Emit(OpCodes.Call, constructorInfo);
				gen.OutDebug("OpCodes.Call {0}", constructorInfo);
			}

			if(!withResult) { 
				gen.Pop();
			}
		}
	}

	public class ILCast : Expr { 

		Expr inputExpression;

		public override Type Type { get; }

		public ILCast(Expr inputExpression, Type outputType) { 
			inputExpression.assetNullType("Cast none type");
			this.inputExpression = inputExpression;
			this.Type = outputType;
		}

		public override void emitPush(ILGen gen) {
			inputExpression.emitPush(gen);
			
			if(!MustBeCasted(inputExpression.Type, Type))
				return;

			//Boxing
			if(Type == typeof(object)) { 
				gen.il.Emit(OpCodes.Box, inputExpression.Type);
				gen.OutDebug("OpCodes.Box {0}", inputExpression.Type);
				return;
			}

			//Cast to number
			NativeTypeInfo castNumericType = NumericOps.GetMSILConvertionType(inputExpression.Type, Type);
			if(castNumericType != null) {
				gen.il.Emit(castNumericType.ConvertionOpCode);
				gen.OutDebug("Convert to {0} opcode", Type);
				return;
			}

			//Cast by user method
			MethodInfo castMethod = ClassesOps.FindCastMethod(inputExpression.Type, Type);
			if(castMethod != null) { 
				gen.OutDebug("OpCodes.Call {0}", castMethod);
				return;
			}

			//Default classes cast
			gen.il.Emit(OpCodes.Castclass, Type);
			gen.OutDebug("OpCodes.Castclass {0}", Type);
		}

		public static bool MustBeCasted(Type inputType, Type targetType) {
			//Boxing
			if(inputType.IsValueType && targetType == typeof(object))
				return true;

			if(targetType.IsAssignableFrom(inputType))
				return false;

			if(NumericOps.IsNumericType(inputType) && NumericOps.IsNumericType(targetType) && NumericOps.GetMSILConvertionType(inputType, targetType) == null)
				return false;

			return true;
		}
	}

	public class BinaryOpFactory { 

		Func<Expr, Expr, Expr> numericOpFactory;
		ClassesOps.BinaryOperation classesOperator;

		public BinaryOpFactory(Func<Expr, Expr, Expr> numericOpFactory, ClassesOps.BinaryOperation classesOperator) { 
			this.numericOpFactory = numericOpFactory;
			this.classesOperator = classesOperator;
		}

		public Expr getExpression(Expr operand1, Expr operand2) {
			//Numbers
			if(NumericOps.IsNumericOp(operand1.Type, operand2.Type)) { 
				return numericOpFactory(operand1, operand2);
			}

			//Overloaded operator
			MethodInfo opMethodInfo = classesOperator.FindMethod(operand1.Type, operand2.Type);
			if(opMethodInfo != null) { 
				return new ILMethodCall(null, opMethodInfo, operand1, operand2);
			}

			//Object references
			if(this == Ops.Equal || this == Ops.NEqual)
				if(!operand1.Type.IsValueType && !operand2.Type.IsValueType)
					return numericOpFactory(operand1, operand2);

			return null;
		}

	}
	
	public class UnaryOpFactory { 

		Func<Expr, Expr> numericOpFactory;
		ClassesOps.UnaryOperation classesOperator;

		public UnaryOpFactory(Func<Expr, Expr> numericOpFactory, ClassesOps.UnaryOperation classesOperator) { 
			this.numericOpFactory = numericOpFactory;
			this.classesOperator = classesOperator;
		}

		public Expr getExpression(Expr operand1) {
			if(NumericOps.IsNumericType(operand1.Type)) { 
				return numericOpFactory(operand1);
			}

			MethodInfo opMethodInfo = classesOperator.FindMethod(operand1.Type);
			if(opMethodInfo != null) { 
				return new ILMethodCall(null, opMethodInfo, operand1);
			}

			return null;
		}

	}

	//Фабрики выражений операторов
	public static partial class Ops { 


		public static BinaryOpFactory Add = new BinaryOpFactory(AddOperationFactory, ClassesOps.OPERATION_ADD);
		public static BinaryOpFactory Sub = new BinaryOpFactory(SubOperationFactory, ClassesOps.OPERATION_SUB);
		public static BinaryOpFactory Mul = new BinaryOpFactory(MulOperationFactory, ClassesOps.OPERATION_MUL);
		public static BinaryOpFactory Div = new BinaryOpFactory(DivOperationFactory, ClassesOps.OPERATION_DIV);
		public static BinaryOpFactory Mod = new BinaryOpFactory(ModOperationFactory, ClassesOps.OPERATION_MOD);

		public static BinaryOpFactory And = new BinaryOpFactory(MulOperationFactory, ClassesOps.OPERATION_BIT_AND);
		public static BinaryOpFactory Or = new BinaryOpFactory(DivOperationFactory, ClassesOps.OPERATION_BIT_OR);
		public static BinaryOpFactory XOr = new BinaryOpFactory(ModOperationFactory, ClassesOps.OPERATION_EX_OR);
		public static BinaryOpFactory RightShift = new BinaryOpFactory(RightShiftOperationFactory, ClassesOps.OPERATION_RIGHT_SHIFT);
		public static BinaryOpFactory LeftShift = new BinaryOpFactory(LeftShiftOperationFactory, ClassesOps.OPERATION_LEFT_SHIFT);

		public static BinaryOpFactory Equal = new BinaryOpFactory(EqualOperationFactory, ClassesOps.OPERATION_EQUAL);
		public static BinaryOpFactory Less = new BinaryOpFactory(LessOperationFactory, ClassesOps.OPERATION_LESS);
		public static BinaryOpFactory Greater = new BinaryOpFactory(GreaterOperationFactory, ClassesOps.OPERATION_GREATER);
		public static BinaryOpFactory NEqual = new BinaryOpFactory(NotEqualOperationFactory, ClassesOps.OPERATION_NEQUAL);
		public static BinaryOpFactory GEqual = new BinaryOpFactory(GEqualOperationFactory, ClassesOps.OPERATION_GEQUAL);
		public static BinaryOpFactory LEqual = new BinaryOpFactory(LEqualOperationFactory, ClassesOps.OPERATION_LEQUAL);

		//Unary operations
		public static UnaryOpFactory Not = new UnaryOpFactory(NotOperationFactory, ClassesOps.OPERATION_NOT);
		public static UnaryOpFactory Inv = new UnaryOpFactory(InverseOperationFactory, ClassesOps.OPERATION_BIT_INVERSE);
		/*
		public static UnaryOperation OPERATION_INC = new UnaryOperation("op_Increment");
		public static UnaryOperation OPERATION_DEC = new UnaryOperation("op_Decrement");
		*/

		
	}

	public class ILReturn : IILEmitable { 

		Expr returnExpression;

		public ILReturn(Expr returnExpression = null) { 
			this.returnExpression = returnExpression;
		}

		public void emit(ILGen gen) {
			if(!gen.IsVoidOrNullReturnType) {
				if(returnExpression.Type != gen.returnMethod.ReturnType)
					throw new InSharpException("False return type");

				returnExpression.emitPush(gen);
				gen.ReturnVar.emitPop(gen);

			}
			gen.il.Emit(OpCodes.Br_S, gen.ReturnLabel);
			gen.OutDebug("OpCodes.Br_S, gen.ReturnLabel");
		}
	}

	public interface ICodeConstruction : IILEmitable { 

		//void EndConstruction(ILGen gen);

	}

	

	public abstract partial class ILGen { 

		private class AutoExpandableStorage { 

			List<ILVar> vars = new List<ILVar>();
			Type varsType;
			ILGen gen;
			int currentIndex;

			public AutoExpandableStorage(Type varsType, ILGen gen) { 
				this.varsType = varsType;
				this.gen = gen;
				currentIndex = 0;
			}

			public ILVar NextVar() { 
				if(vars.Count == currentIndex) { 
					vars.Add(gen.DeclareVar(varsType));
				}
				return vars[currentIndex++];
			}

			public void FreeAll() { 
				currentIndex = 0;
			}

			public void FreeVar() {
				if(currentIndex == 0)
					throw new InSharpException("Nothing to free");
				currentIndex--;
			}

		}

		public ILGenerator il { get; protected set; }
		public ILArg[] args;
		
		public Type ReturnType { get; protected set; }
		public bool IsVoidOrNullReturnType { get => ReturnType == null || ReturnType == typeof(void); }
		public ILVar ReturnVar { get; protected set; } = null;
		public Label ReturnLabel { get; protected set; }
		public DynamicMethod returnMethod;
		//Temporary variables
		private Dictionary<Type, AutoExpandableStorage> temporaryVariables = new Dictionary<Type, AutoExpandableStorage>();
		protected List<IILEmitable> lines = new List<IILEmitable>();

		public bool EnableDebug { get; protected set; } = false;

		public ILVar DeclareVar(Type type) {
			return new ILVar(il.DeclareLocal(type));
		}
		public ILVar DeclareVar<T>() {
			return new ILVar(il.DeclareLocal(typeof(T)));
		}

		public Label DefineLabel() { 
			return il.DefineLabel();
		}

		public void MarkLabel(Label label, string labelName = null) { 
			il.MarkLabel(label);

			if(labelName != null)
				OutComment("{0}:", labelName);
		}

		public ILVar GetTemporaryVariable(Type type) { 
			AutoExpandableStorage storage;
			if(!temporaryVariables.TryGetValue(type, out storage)) { 
				temporaryVariables[type] = storage = new AutoExpandableStorage(type, this);
			}
			return storage.NextVar();
		}

		public void FreeTemporaryVar(Type type) { 
			AutoExpandableStorage storage;
			if(temporaryVariables.TryGetValue(type, out storage)) { 
				storage.FreeVar();
			} else { 
				throw new InSharpException("Nothing to free");
			}
		}

		private void FreeAllVars() { 
			foreach(var pair in temporaryVariables) { 
				pair.Value.FreeAll();
			}
		}

		internal void OutDebug(string formatString, params object[] formatArgs) { 
			if(EnableDebug)
				Console.WriteLine("\t" + formatString, formatArgs);
		}
		internal void OutComment(string formatString, params object[] formatArgs) { 
			if(EnableDebug)
				Console.WriteLine(formatString, formatArgs);
		}


		//--------------------------------Common MSIL commands--------------------------------

		internal void Pop() {
			il.Emit(OpCodes.Pop);
			OutDebug("OpCodes.Pop");
		}

		internal void Dup() {
			il.Emit(OpCodes.Dup);
			OutDebug("OpCodes.Dup");
		}

		internal void Ret() {
			il.Emit(OpCodes.Ret);
			OutDebug("OpCodes.Ret");
		}

		//--------------------------------Code gen--------------------------------

		public Expr Op(Expr expression1, BinaryOpFactory opFactory, Expr expression2) {
			Expr expr = opFactory.getExpression(expression1, expression2);
			if(expr.IsNull())
				throw new InSharpException("Operation isn't allowed");

			return expr;
		}

		public Expr Op(Expr expression1, UnaryOpFactory opFactory) { 
			Expr expr = opFactory.getExpression(expression1);

			if(expr.IsNull())
				throw new InSharpException("Operation isn't allowed");

			return expr;
		}

		public Expr Const(object value) { 
			return new ILConst(value);
		}

		public void Line(IILEmitable statement) { 
			lines.Add(statement);
			FreeAllVars();
		}

		public void Return(Expr returnExpression = null) {
			if(IsVoidOrNullReturnType) { 
				if(returnExpression != null)
					throw new InSharpException("Function doesn't return a value");
			} else { 
				if(returnExpression == null)
					throw new InSharpException("Function returns a value");
			}
				

			Line(new ILReturn(!returnExpression.IsNull() ? returnExpression.CompatiblePass(ReturnType) : null));
		}

		//--------------------------------Constructions--------------------------------

		private Stack<ICodeConstruction> constructions = new Stack<ICodeConstruction>();

		internal void PushConstruction(ICodeConstruction codeConstruction) { 
			constructions.Push(codeConstruction);
		}

		internal ICodeConstruction PopConstruction() { 
			if(constructions.Count == 0)
				throw new InSharpException("No contruction");

			return constructions.Pop();
		}

		internal T GetCurrentCodeConstruction<T>() where T : class, ICodeConstruction { 
			if(constructions.Count == 0)
				throw new InSharpException("No contruction");

			T result = constructions.Peek() as T;
			
			if(result == null)
				throw new InSharpException("False current construction type");

			return result;
		}

		public ICodeConstruction End() {
			return PopConstruction();
		}

		public T End<T>() where T : class, ICodeConstruction {
			if(constructions.Count == 0)
				throw new InSharpException("No contruction");

			T result = constructions.Pop() as T;
			
			if(result == null)
				throw new InSharpException("False current construction type");

			return result;
		}
	}

	public class ILGen<T> : ILGen where T : Delegate {

		public ILGen(string name, bool ignoreVisibility = false) {
			MethodInfo invokeMethod = typeof(T).GetMethod("Invoke");
			ReturnType = invokeMethod.ReturnType;
			Type[] genericArgs = invokeMethod.GetParameters().Select((ParameterInfo parameterInfo) => parameterInfo.ParameterType).ToArray();
			

			returnMethod = new DynamicMethod(name, ReturnType, genericArgs, ignoreVisibility);
			il = returnMethod.GetILGenerator();
			this.args = genericArgs.Select((argType, index) => new ILArg((byte)index, argType)).ToArray();
			
			ReturnLabel = DefineLabel();

			if(!IsVoidOrNullReturnType)
				ReturnVar = DeclareVar(ReturnType);
		}	

		public T compile(bool enableDebug = false) {
			this.EnableDebug = enableDebug;

			OutDebug("\n------------------------Start function \"{0}\"------------------------\n", returnMethod.Name);

			foreach(IILEmitable line in lines) { 
				line.emit(this);
			}

			//Return end
			MarkLabel(ReturnLabel);
			if(!IsVoidOrNullReturnType) { 
				ReturnVar.emitPush(this);
			}
			Ret();

			OutDebug("\n-------------------------End function \"{0}\"-------------------------\n", returnMethod.Name);
			
			return (T)returnMethod.CreateDelegate(typeof(T));
		}
	}

	public static class Counstructions { 

		internal class IfConstruction : ICodeConstruction {

			Label afterLabel;

			List<(Expr expression, Label elseLabel)?> expressions = new List<(Expr expression, Label elseLabel)?>();
			private bool IsLogicalEnded { get => expressions.Last() == null; }

			public IfConstruction(ILGen gen, Expr ifExpression) { 
				afterLabel = gen.DefineLabel();
				expressions.Add((ifExpression, gen.DefineLabel()));
			}

			public void ElseIfStatement(ILGen gen, Expr ifExpression) {
				if(IsLogicalEnded)
					throw new InSharpException("Can't put something after \"Else\"");

				ifExpression.assetNullType("Can't put \"ElseIf\" without expression");

				expressions.Add((ifExpression, gen.DefineLabel()));
			}

			public void ElseStatement() { 
				if(IsLogicalEnded)
					throw new InSharpException("Can't put something after \"Else\"");

				expressions.Add(null);
			}
			int currentEmitIndex = 0;

			public void emit(ILGen gen) {
				//End construction
				if(currentEmitIndex == expressions.Count) { 
					gen.MarkLabel(afterLabel, "AfterLabel");
					return;
				}

				var currentExpression = expressions[currentEmitIndex];

				if(currentEmitIndex == 0) { //First expression
					currentExpression.Value.expression.emitPush(gen);
					gen.il.Emit(OpCodes.Brfalse, currentExpression.Value.elseLabel);
					gen.OutDebug("OpCodes.Brfalse ElseLabel" );
				} else { 
					var lastExpression = expressions[currentEmitIndex - 1];
					if(currentExpression != null) {
						gen.il.Emit(OpCodes.Br, afterLabel); 
						gen.OutDebug("OpCodes.Br AfterLabel");
						gen.MarkLabel(lastExpression.Value.elseLabel, "ElseLabel");

						currentExpression.Value.expression.emitPush(gen);
						gen.il.Emit(OpCodes.Brfalse, currentExpression.Value.elseLabel);
						gen.OutDebug("OpCodes.Brfalse ElseLabel");
					} else { 
						gen.il.Emit(OpCodes.Br, afterLabel); 
						gen.OutDebug("OpCodes.Br After_label");
						gen.MarkLabel(lastExpression.Value.elseLabel, "ElseLabel");
					}
				}

				currentEmitIndex++;
			}
		}

		public static void If(this ILGen gen, Expr ifExpression) {
			IfConstruction ifConstruction = new IfConstruction(gen, ifExpression);
			gen.PushConstruction(ifConstruction);

			gen.Line(ifConstruction);

		}

		public static void ElseIf(this ILGen gen, Expr ifExpression) {
			IfConstruction ifConstruction = gen.GetCurrentCodeConstruction<IfConstruction>();
			ifConstruction.ElseIfStatement(gen, ifExpression);

			gen.Line(ifConstruction);
		}

		public static void Else(this ILGen gen) {
			IfConstruction ifConstruction = gen.GetCurrentCodeConstruction<IfConstruction>();
			ifConstruction.ElseStatement();

			gen.Line(ifConstruction);
		}

		public static void EndIf(this ILGen gen) { 
			gen.Line(gen.End<IfConstruction>());
		}

		internal class WhileConstruction : ICodeConstruction {

			Expr whileExpression;
			Label startLabel;
			Label elseLabel;

			public WhileConstruction(ILGen gen, Expr whileExpression) { 
				this.whileExpression = whileExpression;
				this.startLabel = gen.il.DefineLabel();
				this.elseLabel = gen.il.DefineLabel();
			}

			int currentEmitIndex = 0;

			public void emit(ILGen gen) {
				if(currentEmitIndex == 0) { 
					gen.MarkLabel(startLabel, "StartLabel");
					whileExpression.emitPush(gen);
					gen.il.Emit(OpCodes.Brfalse, elseLabel);
					gen.OutDebug("OpCodes.Brfalse ElseLabel");
				} else if(currentEmitIndex == 1) {
					gen.il.Emit(OpCodes.Br, startLabel);
					gen.OutDebug("OpCodes.Br StartLabel");
					gen.MarkLabel(elseLabel, "ElseLabel");
				}


				currentEmitIndex++;
			}
		}

		public static void While(this ILGen gen, Expr whileExpression) {
			WhileConstruction whileConstruction = new WhileConstruction(gen, whileExpression);
			gen.PushConstruction(whileConstruction);
			gen.Line(whileConstruction);
		}

		public static void EndWhile(this ILGen gen) { 
			gen.Line(gen.End<WhileConstruction>());
		}
	}

	public static class ArraysExtensions { 
		public static void SetArrayValues(this ILGen gen, ILAssignable arrayAssignable, int[] sizes, params Expr[] values) {
			arrayAssignable.assetNullType();
			arrayAssignable.assetCheckArrayType();

			if(arrayAssignable.Type.GetArrayRank() != sizes.Length)
				throw new InSharpException("Invalis dimensions count");


			if(sizes.Aggregate((a, b) => a * b) != values.Length)
				throw new InSharpException("Invalis values count");


			int[] currentPoint = new int[sizes.Length];
			for(int valueIndex = 0; valueIndex < values.Length; valueIndex++) { 

				int buffer = valueIndex;
				for(int dimIndex = sizes.Length - 1; dimIndex >= 0; dimIndex++) { 
					int dimSize = sizes[dimIndex];
					currentPoint[dimIndex] = buffer % dimSize;
					buffer = dimIndex / dimSize;
				}

				gen.Line(arrayAssignable.Index(currentPoint.Select((pos) => (Expr)(new ILConst(pos))).ToArray()).Set(values[valueIndex]) );
			}

			
		}
	}

	public class InSharpException : Exception { 
		public InSharpException(string message, params object[] args) : base(string.Format(message, args)) {  }
	}
}
