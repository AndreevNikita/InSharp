using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

/*
 * TODO
 +- Конвертация
 - Подготовка к присваиванию (pass)
 +- Вызов методов с переменным количеством аргументов
 - Оптимизация констант и известных типов
 - Многомерные массивы
 */

namespace _ILShell
{
	public abstract class ILRes {
		public abstract void push(ILGen gen);

		public virtual void pushRef(ILGen gen) { 
			throw new InSharpException("Object hasn't ref");
		}

		public abstract bool IsValueType { get; }
		public abstract Type Type { get; }

		public ILFieldsFactory Fields { get; private set; }
		public ILPropertiesFactory Properties { get; private set; }

		public ILArrayMethods AsArray { get; private set; }

		public ILRes() { 
			Fields = new ILFieldsFactory(this);
			Properties = new ILPropertiesFactory(this);
			AsArray = new ILArrayMethods(this);
		}

		public static implicit operator ILRes(int value) { 
			return new ILConst<int>(value);
		} 

		public static implicit operator ILRes(bool value) { 
			return new ILConst<bool>(value);
		} 

		public static implicit operator ILRes(string value) { 
			return new ILConst<string>(value);
		}


		public class ILFieldsFactory { 
			ILRes owner;

			internal ILFieldsFactory(ILRes owner) { 
				this.owner = owner;
			}

			public ILField shell(FieldInfo fieldInfo) { 
				return new ILField(owner, fieldInfo);
			}

			public ILField this[string name] {
				get { 
					return new ILField(owner, owner.Type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
				}
			}
		}

		public class ILPropertiesFactory { 
			ILRes owner;

			internal ILPropertiesFactory(ILRes owner) { 
				this.owner = owner;
			}

			public ILProperty shell(PropertyInfo propertyInfo) { 
				return new ILProperty(owner, propertyInfo);
			}

			public ILProperty this[string name] {
				get { 
					return new ILProperty(owner, owner.Type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
				}
			}
		}

		public ILFuncRes callMethod(MethodInfo methodInfo, params ILRes[] args) {
			return new ILFuncRes(this, methodInfo, args);
		}

		public class ILArrayMethods { 
			ILRes owner;

			public ILRes Length { get { return new ILArrayLength(owner); } }

			internal ILArrayMethods(ILRes owner) { 
				this.owner = owner;
			}

			public ILArrayIndex this[ILRes indexRes] {
				get { 
					return new ILArrayIndex(owner, indexRes);
				}
			}
		}
	}

	public abstract class ILWritable : ILRes {

		public abstract void prePop(ILGen gen);
		public abstract void pop(ILGen gen);
	}

	public class ILFakeRes : ILRes { 

		Type tType;
		public override bool IsValueType { get => tType.IsValueType; } 
		public override Type Type { get => tType; } 

		public ILFakeRes(Type type) { 
			this.tType = type;
		}

		public override void push(ILGen gen) {
			if (tType == null) {
				throw new InSharpException("No return type");
			}

			//gen.il.Emit(OpCodes.Ldloc, localVariable);
		}
	}
	public class ILMethod {
		
		private ILRes instance;
		private MethodInfo methodInfo;

		public ILMethod(ILRes instance, MethodInfo methodInfo) {
			this.instance = instance;
			this.methodInfo = methodInfo;
		}

		public ILFuncRes call(params ILRes[] args) { 
			return new ILFuncRes(instance, methodInfo, args);
		}
	}

	
	
	public class ILFuncRes : ILRes { 
		Type tType;
		public override bool IsValueType { get => tType.IsValueType; } 
		public override Type Type { get => tType; } 

		ILRes ownerInstance;
		ILRes[] args;
		MethodInfo methodInfo;

		public ILFuncRes(ILRes ownerInstance, MethodInfo methodInfo, ILRes[] args) { 
			this.tType = methodInfo.ReturnType;

			this.ownerInstance = ownerInstance;
			this.args = args;
			this.methodInfo = methodInfo;
		}

		public override void push(ILGen gen) {
			if(gen.EnableDebug)
				Console.WriteLine("\n//Call method");
			if(ownerInstance != null) { 
				ownerInstance.pushRef(gen);
			}

			ParameterInfo[] parameters = methodInfo.GetParameters();
			for(int index = 0; index != parameters.Length; index++) {
				if(index == parameters.Length - 1) { //For last element
					ParameterInfo lastParameterInfo = parameters[index];
					if(lastParameterInfo.GetCustomAttribute(typeof(ParamArrayAttribute), false) != null) {
						Type attribsTypes = lastParameterInfo.ParameterType.GetElementType();
						int paramsCount = args.Length - parameters.Length + 1; //Find count of params arguments

						//Create params arguments array
						gen.il.Emit(OpCodes.Ldc_I4, paramsCount);
						gen.il.Emit(OpCodes.Newarr);
						if(gen.EnableDebug) { 
							Console.WriteLine("OpCodes.Ldc_I4 {0}", paramsCount);
							Console.WriteLine("OpCodes.Newarr");
						}
								
						for(int argIndex = index; argIndex < args.Length; argIndex++) { 
							gen.il.Emit(OpCodes.Dup);
							if(gen.EnableDebug)
								Console.WriteLine("OpCodes.Dup");

							args[argIndex].push(gen);
							if(attribsTypes.IsValueType) {
								gen.il.Emit(OpCodes.Stelem, attribsTypes);
								if(gen.EnableDebug)
									Console.WriteLine("OpCodes.Stelem {0}", attribsTypes.FullName);
							} else {
								gen.il.Emit(OpCodes.Stelem_Ref);
								if(gen.EnableDebug)
									Console.WriteLine("OpCodes.Stelem_Ref");
										
							}
						}
						break;
					}
				} else { 
					args[index].push(gen);
				}
			}

			if(ownerInstance == null || ownerInstance.IsValueType) {
				gen.il.Emit(OpCodes.Call, methodInfo);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Call {0}", methodInfo);
			} else { 
				gen.il.Emit(OpCodes.Callvirt, methodInfo);
				if(gen.EnableDebug) {
					Console.WriteLine("OpCodes.Callvirt {0}", methodInfo);
				}
			}
			//gen.il.Emit(OpCodes.Ldloc, localVariable);
		}
	}

	public class ILNewRes : ILRes { 
		Type tType;
		public override bool IsValueType { get => tType.IsValueType; } 
		public override Type Type { get => tType; } 

		ILRes[] args;
		ConstructorInfo constructorInfo;

		public ILNewRes(ConstructorInfo constructorInfo, ILRes[] args) { 
			this.tType = constructorInfo.DeclaringType;

			this.args = args;
			this.constructorInfo = constructorInfo;
		}

		public override void push(ILGen gen) {
			if(gen.EnableDebug)
				Console.WriteLine("\n//Call Constructor");

			ParameterInfo[] parameters = constructorInfo.GetParameters();
			for(int index = 0; index < args.Length; index++) {
				args[index].push(gen);
				
				if(parameters[index].ParameterType == typeof(object) && args[index].IsValueType) {
					gen.il.Emit(OpCodes.Box, typeof(int));
					if(gen.EnableDebug)
						Console.WriteLine("OpCodes.Box");
				}
			}

			if(!IsValueType) {
				gen.il.Emit(OpCodes.Newobj, constructorInfo);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Newobj {0}", constructorInfo);
			} else {
				gen.il.Emit(OpCodes.Call, constructorInfo);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Call {0}", constructorInfo);
			}
			//gen.il.Emit(OpCodes.Ldloc, localVariable);
		}
	}

	public class ILCreateArray : ILRes { 
		Type elementType;
		Type tType;
		public override bool IsValueType { get => false; } 
		public override Type Type { get => tType; } 

		ILRes size;

		public ILCreateArray(Type elementType, ILRes size) { 
			this.elementType = elementType;
			this.tType = elementType.MakeArrayType();
			this.size = size;
		}

		public override void push(ILGen gen) {
			size.push(gen);
			gen.il.Emit(OpCodes.Newarr, elementType);
		}
	}

	public class ILArrayIndex : ILWritable { 
		public override bool IsValueType { get => Type.IsValueType; } 
		public override Type Type { get => instance.Type.GetElementType(); } 

		public readonly ILRes instance;
		public readonly ILRes indexRes;

		public ILArrayIndex(ILRes instance, ILRes indexRes) { 
			this.instance = instance;
			this.indexRes = indexRes;
		}

		public override void prePop(ILGen gen) { 
			instance.push(gen);
			indexRes.push(gen);
		}

		public override void pop(ILGen gen) { 
			if(Type.IsValueType) { 
				gen.il.Emit(OpCodes.Stelem, Type);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Stelem, {0}", Type);
			} else { 
				gen.il.Emit(OpCodes.Stelem_Ref);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Stelem_Ref");
			}
			
			//OpCodes.Stelem_I4
			//if(gen.EnableDebug)
			//	Console.WriteLine("(OpCodes.Stelem_I4 {0}", null);
		}

		public override void push(ILGen gen) {
			instance.push(gen);
			indexRes.push(gen);

			if(Type.IsValueType) { 
				gen.il.Emit(OpCodes.Ldelem, Type);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Ldelem, {0}", Type);
			} else { 
				gen.il.Emit(OpCodes.Ldelem_Ref);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Ldelem_Ref");
			}
		}
	}

	public class ILArrayLength : ILRes {
		
		public override bool IsValueType { get => Type.IsValueType; } 
		public override Type Type { get => typeof(int); } 

		public readonly ILRes instance;

		public ILArrayLength(ILRes instance) { 
			this.instance = instance;
		}

		public override void push(ILGen gen) {
			instance.push(gen);
			gen.il.Emit(OpCodes.Ldlen);
			gen.il.Emit(OpCodes.Conv_I4);
			if(gen.EnableDebug) {
				Console.WriteLine("OpCodes.Ldlen");
				Console.WriteLine("OpCodes.Conv_I4");
			}
		}
	}

	public class ILField : ILWritable { 
		public override bool IsValueType { get => Type.IsValueType; } 
		public override Type Type { get => field.FieldType; } 

		public readonly ILRes instance;
		public readonly FieldInfo field;

		public ILField(ILRes instance, FieldInfo field) { 
			this.field = field;
			this.instance = instance;
		}

		public override void prePop(ILGen gen) { 
			if(instance != null)
				instance.pushRef(gen);
		}

		public override void pop(ILGen gen) { 
			gen.il.Emit(OpCodes.Stfld, field);
			if(gen.EnableDebug)
				Console.WriteLine("OpCodes.Stfld {0}", field);
		}

		public override void push(ILGen gen) {
			if(instance != null)
				instance.push(gen);

			gen.il.Emit(OpCodes.Ldfld, field);
			if(gen.EnableDebug)
				Console.WriteLine("OpCodes.Ldfld {0}", field);
		}
	}

	public class ILProperty : ILWritable { 
		public override bool IsValueType { get => Type.IsValueType; } 
		public override Type Type { get => property.PropertyType; } 

		public readonly ILRes instance;
		public readonly PropertyInfo property;
		public readonly MethodInfo getMethod;
		public readonly MethodInfo setMethod;

		public ILProperty(ILRes instance, PropertyInfo property) { 
			this.property = property;
			this.getMethod = property.GetMethod;
			this.setMethod = property.SetMethod;
			this.instance = instance;
		}

		public override void prePop(ILGen gen) { 
			if(instance != null)
				instance.pushRef(gen);
		}

		public override void pop(ILGen gen) { 
			if(instance != null && !instance.IsValueType) { 
				gen.il.Emit(OpCodes.Callvirt, setMethod);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Callvirt {0}", setMethod);
			} else { 
				gen.il.Emit(OpCodes.Call, setMethod);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Call {0}", setMethod);
			}
			
		}

		public override void push(ILGen gen) {
			if(instance != null) { 
				instance.push(gen);
				gen.il.Emit(OpCodes.Callvirt, getMethod);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Callvirt {0}", getMethod);
			} else { 
				gen.il.Emit(OpCodes.Call, getMethod);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Call {0}", getMethod);
			}
				
		}
	}

	
	public class ILConst<T> : ILRes { 
		T value;
		Type tType;
		public override Type Type { get => tType; } 

		public override bool IsValueType { get => tType.IsValueType; }

		public ILConst(T value) {
			this.value = value;
			this.tType = typeof(T);
		}

		public override void push(ILGen gen) {
			
			if(tType == typeof(int)) { 
				gen.il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(value));
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Ldc_I4, {0}", value);
			} else if(tType == typeof(bool)) { 
				gen.il.Emit(Convert.ToBoolean(value) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
				if(gen.EnableDebug)
					Console.WriteLine(Convert.ToBoolean(value) ? "OpCodes.Ldc_I4_1" : "OpCodes.Ldc_I4_0");
			} else if(tType == typeof(string)) {
				gen.il.Emit(OpCodes.Ldstr, Convert.ToString(value));
				if (gen.EnableDebug)
					Console.WriteLine("OpCodes.Ldstr, \"{0}\"", value);
			}
			
			
				
		} 
	}

	public class ILVar : ILWritable {

		protected LocalBuilder localVariable;

		public override Type Type { get => localVariable.LocalType; } 
		public override bool IsValueType { get => localVariable.LocalType.IsValueType; }

		public ILVar(LocalBuilder localVariable) {
			this.localVariable = localVariable;
		}

		public override void push(ILGen gen) {
			gen.il.Emit(OpCodes.Ldloc, localVariable);
			if(gen.EnableDebug)
				Console.WriteLine("OpCodes.Ldloc, {0}", localVariable);
		}

		public override void pushRef(ILGen gen) { 
			if(!IsValueType) { 
				push(gen);
			} else { 
				gen.il.Emit(OpCodes.Ldloca_S, localVariable);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Ldloca_S, {0}", localVariable);
			}
		}

		public override void prePop(ILGen gen) { }

		public override void pop(ILGen gen) { 
			gen.il.Emit(OpCodes.Stloc, localVariable);
			if(gen.EnableDebug)
				Console.WriteLine("OpCodes.Stloc, {0}", localVariable);
		}
	}


	public class ILArg : ILWritable { 
		
		Type tType;
		protected readonly byte index;

		public override Type Type { get => tType; } 
		public override bool IsValueType { get => tType.IsValueType; }

		public ILArg(byte index, Type tType) {
			this.index = index;
			this.tType = tType;
		}

		public override void push(ILGen gen) {
			gen.il.Emit(OpCodes.Ldarg_S, index);
			if(gen.EnableDebug)
				Console.WriteLine("OpCodes.Ldarg_S, {0}", index);
		}

		public override void pushRef(ILGen gen) { 
			if(!IsValueType) { 
				push(gen);
			} else { 
				gen.il.Emit(OpCodes.Ldarga_S, index);
				if(gen.EnableDebug)
					Console.WriteLine("OpCodes.Ldarga_S, {0}", index);
			}
		}
		public override void prePop(ILGen gen) { }

		public override void pop(ILGen gen) { 
			gen.il.Emit(OpCodes.Starg_S, index);
			if(gen.EnableDebug)
				Console.WriteLine("OpCodes.Starg_S, {0}", index);
		}
	}

	public class ILConvert : ILRes {
		
		ILRes what;
		Type to;

		private static Dictionary<Type, OpCode> convertionOpcodes = new Dictionary<Type, OpCode> { 
			{ typeof(double),	OpCodes.Conv_R8 },
			{ typeof(float),	OpCodes.Conv_R4 },
			{ typeof(Int64),	OpCodes.Conv_I8 },
			{ typeof(Int32),	OpCodes.Conv_I4 },
			{ typeof(Int16),	OpCodes.Conv_I2 },
			{ typeof(sbyte),	OpCodes.Conv_I1 },
			{ typeof(UInt64),	OpCodes.Conv_U8 },
			{ typeof(UInt32),	OpCodes.Conv_U4 },
			{ typeof(UInt16),	OpCodes.Conv_U2 },
			{ typeof(byte),		OpCodes.Conv_U1 },
		};

		public override Type Type { get => to; } 
		public override bool IsValueType { get => to.IsValueType; }

		public ILConvert(ILRes what, Type to) { 
			this.what = what;
			this.to = to;
		}

		public override void push(ILGen gen) { 
			what.push(gen);

			if(to == typeof(object)) { 
				if(what.IsValueType) { 
					gen.il.Emit(OpCodes.Box, what.Type);
				} else { 
					//Do nothing
				}
			} else if(!IsValueType) { //Class to class 
				if(to.IsAssignableFrom(what.Type)) { 
					//Do nothing
				} else {
					gen.il.Emit(OpCodes.Castclass, to);
				}
			} else { 
				OpCode opcode;
				if(!convertionOpcodes.TryGetValue(to, out opcode)) {
					throw new InSharpException("Can't convert to " + to.Name);
				}
				gen.il.Emit(opcode);
			}
		}

	}

	public abstract class ILGen { 
		
		public readonly static ILBinaryOperator Add = new ILBinaryOperator(Operator.ReturnType.Numeric, 
			(gen, op1, op2) => { 
				op1.push(gen);
				op2.push(gen);
				gen.il.Emit(OpCodes.Add); if(gen.EnableDebug) Console.WriteLine("Add");
			}
		);
		public readonly static ILBinaryOperator Sub = new ILBinaryOperator(Operator.ReturnType.Numeric,
			(gen, op1, op2) => { 
				op1.push(gen);
				op2.push(gen);
				gen.il.Emit(OpCodes.Sub); if(gen.EnableDebug) Console.WriteLine("Sub");
			}
		);
		public readonly static ILBinaryOperator Mul = new ILBinaryOperator(Operator.ReturnType.Numeric,
			(gen, op1, op2) => { 
				op1.push(gen);
				op2.push(gen);
				gen.il.Emit(OpCodes.Mul); if(gen.EnableDebug) Console.WriteLine("Mul");
			}
		);
		public readonly static ILBinaryOperator Div = new ILBinaryOperator(Operator.ReturnType.Numeric,
			(gen, op1, op2) => { 
				op1.push(gen);
				op2.push(gen);
				gen.il.Emit(OpCodes.Div); if(gen.EnableDebug) Console.WriteLine("Div");
			}
		);
		
		public readonly static ILUnaryOperator Inc = new ILUnaryOperator(Operator.ReturnType.Numeric,
			(gen, op) => { 
				op.push(gen);
				gen.il.Emit(OpCodes.Ldc_I4_1);
				gen.il.Emit(OpCodes.Add); 
				if(gen.EnableDebug) {
					Console.WriteLine("Ldc_I4_1");
					Console.WriteLine("Add");
				}
			}
		);

		public readonly static ILBinaryOperator Equal = new ILBinaryOperator(Operator.ReturnType.Boolean,
			(gen, op1, op2) => { 
				op1.push(gen);
				op2.push(gen);
				gen.il.Emit(OpCodes.Ceq); if(gen.EnableDebug) Console.WriteLine("Ceq");
			}
		);
		public readonly static ILBinaryOperator Less = new ILBinaryOperator(Operator.ReturnType.Boolean,
			(gen, op1, op2) => { 
				op1.push(gen);
				op2.push(gen);
				gen.il.Emit(OpCodes.Clt); if(gen.EnableDebug) Console.WriteLine("Clt");
			}
		);
		public readonly static ILBinaryOperator Greater = new ILBinaryOperator(Operator.ReturnType.Boolean,
			(gen, op1, op2) => { 
				op1.push(gen);
				op2.push(gen);
				gen.il.Emit(OpCodes.Cgt); if(gen.EnableDebug) Console.WriteLine("Cgt");
			}
		);

		public readonly static ILUnaryOperator Not = new ILUnaryOperator(Operator.ReturnType.Boolean,
			(gen, op) => { 
				op.push(gen);
				gen.il.Emit(OpCodes.Ldc_I4_0);
				gen.il.Emit(OpCodes.Cgt); if(gen.EnableDebug) Console.WriteLine("Cgt");
			}
		);
		
		public readonly static ILBinaryOperator NEqual = new ILBinaryOperator(Operator.ReturnType.Boolean,
			(gen, op1, op2) => { 
				Equal.genFunc(gen, op1, op2);
				gen.il.Emit(OpCodes.Ldc_I4_0); if(gen.EnableDebug) Console.WriteLine("Ldc_I4_0");
				gen.il.Emit(OpCodes.Ceq); if(gen.EnableDebug) Console.WriteLine("Ceq");
			}
		);
		public readonly static ILBinaryOperator LEqual = new ILBinaryOperator(Operator.ReturnType.Boolean,
			(gen, op1, op2) => { 
				Greater.genFunc(gen, op1, op2);
				gen.il.Emit(OpCodes.Ldc_I4_0); if(gen.EnableDebug) Console.WriteLine("Ldc_I4_0");
				gen.il.Emit(OpCodes.Ceq); if(gen.EnableDebug) Console.WriteLine("Ceq");
			}
		);

		public readonly static ILBinaryOperator GEqual = new ILBinaryOperator(Operator.ReturnType.Boolean,
			(gen, op1, op2) => { 
				Less.genFunc(gen, op1, op2);
				gen.il.Emit(OpCodes.Ldc_I4_0); if(gen.EnableDebug) Console.WriteLine("Ldc_I4_0");
				gen.il.Emit(OpCodes.Ceq); if(gen.EnableDebug) Console.WriteLine("Ceq");
			}
		);
		

		public DynamicMethod returnMethod;

		public ILGenerator il;
		public int stackCounter = 0;
		public ILArg[] args;
		public bool EnableDebug { get; protected set; } = false;

		public Stack<Action> constructinEnds = new Stack<Action>();

		public ILVar declareVar(Type type) {
			return new ILVar(il.DeclareLocal(type));
		}

		public void endBlock() {
			constructinEnds.Pop()();
		}

		public void markLabel(Label label) { 
			il.MarkLabel(label);
		}


		public ILRes op(ILRes operand1, ILBinaryOperator op, ILRes operand2) { 
			return op.enable(operand1, operand2); 
		}

		public ILRes op(ILUnaryOperator op, ILRes operand) { 
			return op.enable(operand); 
		}

		public ILRes convert(ILRes res, Type toType) { 
			return new ILConvert(res, toType);
		}

		public ILField staticField(Type type, string name) { 
			return staticField(type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
		}

		public ILField staticField(FieldInfo fieldInfo) { 
			return new ILField(null, fieldInfo);
		} 

		public ILProperty staticProperty(Type type, string name) { 
			return staticProperty(type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
		}

		public static ILProperty staticProperty(PropertyInfo property) { 
			return new ILProperty(null, property);
		}


		public ILRes newObj(Type type, Type[] argsTypes, params ILRes[] args) { 
			return newObj(type.GetConstructor(argsTypes), args);
		}

		public ILRes newObj(ConstructorInfo constructor, params ILRes[] args) { 
			return new ILNewRes(constructor, args);
		}

		public ILRes newArray(Type arrayType, ILRes size) { 
			return new ILCreateArray(arrayType, size);
		}
	}

	public class ILGen<T> : ILGen where T : Delegate {

		public ILGen(string name, bool ignoreVisibility = false, bool enableDebug = false) {
			Type returnType = typeof(T).GetMethod("Invoke").ReturnType;
			Type[] genericArgs = typeof(T).GetMethod("Invoke").GetParameters().Select((ParameterInfo parameterInfo) => parameterInfo.ParameterType).ToArray();
			

			returnMethod = new DynamicMethod(name, returnType, genericArgs, ignoreVisibility);
			il = returnMethod.GetILGenerator();
			this.args = genericArgs.Select((argType, index) => new ILArg((byte)index, argType)).ToArray();
			this.EnableDebug = enableDebug;

			if(EnableDebug) { 
				Console.WriteLine("\n------------------------Start function \"{0}\"------------------------\n", returnMethod.Name);
			}
		}	

		public T compile() { 
			if(EnableDebug) { 
				Console.WriteLine("\n------------------------End function \"{0}\"------------------------\n", returnMethod.Name);
			}
			return (T)returnMethod.CreateDelegate(typeof(T));
		}
	}

	public class Operator { 
		public enum ReturnType { Numeric, Boolean }
	}

	public class ILUnaryOperator { 
		public readonly Operator.ReturnType returnType;

		public ILUnaryOperator(Operator.ReturnType returnType, Action<ILGen, ILRes> genFunc) { 
			this.returnType = returnType;
			this.genFunc = genFunc;
		}

		public virtual ILRes enable(ILRes operand) {
			return new ILUnaryOperatorRes(this, operand);
		}

		public Action<ILGen, ILRes> genFunc;
	}

	public class ILUnaryOperatorRes : ILRes { 

		public ILUnaryOperator unOperator;
		ILRes operand;

		public ILUnaryOperatorRes(ILUnaryOperator unOperator, ILRes operand) { 
			this.unOperator = unOperator;
			this.operand = operand;
		}

		public override bool IsValueType { 
			get { 
				switch(unOperator.returnType) {
					case Operator.ReturnType.Numeric:
					case Operator.ReturnType.Boolean:
						return true;
					default:
						return false;
				}
			} 
		}

		public override Type Type { 
			get { 
				switch(unOperator.returnType) {
					case Operator.ReturnType.Numeric:
						return typeof(int);
					case Operator.ReturnType.Boolean:
						return typeof(bool);
					default:
						return typeof(object);
				}
			} 
		}

		public override void push(ILGen gen) {
			unOperator.genFunc(gen, operand);
		}
	}

	public class ILBinaryOperator { 
		public readonly Operator.ReturnType returnType;

		public ILBinaryOperator(Operator.ReturnType returnType, Action<ILGen, ILRes, ILRes> genFunc) { 
			this.returnType = returnType;
			this.genFunc = genFunc;
		}

		public virtual ILRes enable(ILRes operand1, ILRes operand2) {
			return new ILBinaryOperatorRes(this, operand1, operand2);
		}

		public Action<ILGen, ILRes, ILRes> genFunc;
	}


	public class ILBinaryOperatorRes : ILRes { 

		public ILBinaryOperator binOperator;
		ILRes operand1;
		ILRes operand2;

		public ILBinaryOperatorRes(ILBinaryOperator binOperator, ILRes operand1, ILRes operand2) { 
			this.binOperator = binOperator;
			this.operand1 = operand1;
			this.operand2 = operand2;
		}

		public override bool IsValueType { 
			get { 
				switch(binOperator.returnType) {
					case Operator.ReturnType.Numeric:
					case Operator.ReturnType.Boolean:
						return true;
					default:
						return false;
				}
			} 
		}

		public override Type Type { 
			get { 
				switch(binOperator.returnType) {
					case Operator.ReturnType.Numeric:
						return typeof(int);
					case Operator.ReturnType.Boolean:
						return typeof(bool);
					default:
						return typeof(object);
				}
			} 
		}

		public override void push(ILGen gen) {
			binOperator.genFunc(gen, operand1, operand2);
			/*operand1.push(gen);
			operand2.push(gen);
			gen.il.Emit(binOperator.opcode);
			if(gen.EnableDebug)
				Console.WriteLine("Binary operator");*/
		}
	}

	public static class ILGenMethods { 

		//--------------------------------Expressions--------------------------------

		public static ILGen inc(this ILGen gen, ILWritable operand1) { 
			operand1.prePop(gen);
			operand1.push(gen);
			gen.il.Emit(OpCodes.Ldc_I4_1);
			gen.il.Emit(OpCodes.Add);
			if(gen.EnableDebug) {
				Console.WriteLine("OpCodes.Ldc_I4_1");
				Console.WriteLine("OpCodes.Add");
			}
			operand1.pop(gen);
			return gen;
		}
		public static ILGen dec(this ILGen gen, ILWritable operand1) { 
			operand1.prePop(gen);
			operand1.push(gen);
			gen.il.Emit(OpCodes.Ldc_I4_1);
			gen.il.Emit(OpCodes.Sub);
			if(gen.EnableDebug) {
				Console.WriteLine("OpCodes.Ldc_I4_1");
				Console.WriteLine("OpCodes.Sub");
			}
			operand1.pop(gen);
			return gen;
		}
		

		//--------------------------------Lines--------------------------------

		/*
		public static ILGen setField(this ILGen gen, ILRes instance, string name, ILRes res) { 
			return gen.setField(instance, instance.Type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static), res);
		}

		public static ILGen setField(this ILGen gen, ILRes instance, FieldInfo field, ILRes res) {
			instance.push(gen);
			res.push(gen);
			gen.il.Emit(OpCodes.Stfld, field);
			if(gen.EnableDebug)
				Console.WriteLine("OpCodes.Stfld {0}", field);
			return gen;
		}*/

		public static ILGen set(this ILGen gen, ILWritable variable, ILRes res) { 
			if(res is ILNewRes && variable.IsValueType) {
				Console.WriteLine("set value type");
				variable.pushRef(gen);
				res.push(gen);
			} else { 
				variable.prePop(gen);
				res.push(gen);
				variable.pop(gen);
			}
			return gen;
		}

		public static ILGen line(this ILGen gen, ILRes res) { 
			res.push(gen);
			if(res.Type != typeof(void)) {
				gen.il.Emit(OpCodes.Pop);
				if(gen.EnableDebug)
				Console.WriteLine("OpCodes.Pop");
			}
			return gen;
		}

		public static ILGen ret(this ILGen gen, ILRes res = null) { 
			if(gen.EnableDebug)
				Console.WriteLine("\n//Return");
			if(res != null)
				res.push(gen);
			gen.il.Emit(OpCodes.Ret);
			if(gen.EnableDebug)
				Console.WriteLine("ret");
			return gen;
		}

		//--------------------------------Constructions--------------------------------

		public static ILGen startIf(this ILGen gen, ILRes res) {
			if(gen.EnableDebug) Console.WriteLine("\n//If:");

			Label elseLabel = gen.il.DefineLabel();
			res.push(gen);
			gen.il.Emit(OpCodes.Brfalse, elseLabel); if(gen.EnableDebug) Console.WriteLine("OpCodes.Brfalse elseLabel");
			gen.constructinEnds.Push(
				() => { 
					gen.markLabel(elseLabel); if(gen.EnableDebug) Console.WriteLine("elseLabel:");
				}
			);
			return gen;
		}

		public static ILGen startIfElse(this ILGen gen, ILRes res) {
			if(gen.EnableDebug) Console.WriteLine("\n//If else:");

			Label elseLabel = gen.il.DefineLabel();
			Label afterLabel = gen.il.DefineLabel();

			//if(res)
			res.push(gen);
			gen.il.Emit(OpCodes.Brfalse, elseLabel); if(gen.EnableDebug) Console.WriteLine("OpCodes.Brfalse elseLabel");
			//...
			
			//After
			gen.constructinEnds.Push(
				() => {
					gen.markLabel(afterLabel); if(gen.EnableDebug) Console.WriteLine("afterLabel:");
				}
			);

			//else
			gen.constructinEnds.Push(
				() => {
					gen.il.Emit(OpCodes.Br, afterLabel); if(gen.EnableDebug) Console.WriteLine("OpCodes.Br afterLabel");
					gen.markLabel(elseLabel); if(gen.EnableDebug) Console.WriteLine("elseLabel:");
				}
			);
			return gen;
		}

		public static ILGen startWhile(this ILGen gen, ILRes res) {
			if(gen.EnableDebug) Console.WriteLine("\n//While:");

			Label startLabel = gen.il.DefineLabel();
			Label elseLabel = gen.il.DefineLabel();
			gen.markLabel(startLabel);  if(gen.EnableDebug) Console.WriteLine("startLabel:");
			res.push(gen);
			gen.il.Emit(OpCodes.Brfalse, elseLabel); if(gen.EnableDebug) Console.WriteLine("OpCodes.Brfalse elseLabel");
			gen.constructinEnds.Push(
				() => {
					gen.il.Emit(OpCodes.Br, startLabel); if(gen.EnableDebug) Console.WriteLine("OpCodes.Br startLabel");
					gen.markLabel(elseLabel); if(gen.EnableDebug) Console.WriteLine("elseLabel:");
				}
			);
			return gen;
		}
	}

	public class InSharpException : Exception { 
		public InSharpException(string message) : base(message) {  }
	}
}
