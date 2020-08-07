using LowLevelOpsHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static LowLevelOpsHelper.NumericOps;

namespace InSharp {
	public class ILArithmeticBinaryOp : Expr {
		Expr operand1, operand2;

		Action<Expr, Expr, ILGen> opAction;

		public override Type Type { get; }

		public ILArithmeticBinaryOp(Expr operand1, Expr operand2, Action<Expr, Expr, ILGen> opAction) {
			this.opAction = opAction;
			NativeTypeInfo outputType =  NumericOps.GetArithmOpResultType(operand1.Type, operand2.Type);
			if(outputType == null)
				throw new InSharpException("Not available operands \"{0}\" & \"{1}\"", operand1, operand2);
			Type = outputType.Type;

			this.operand1 = operand1.CompatiblePass(Type);
			this.operand2 = operand2.CompatiblePass(Type);
		}

		public override void emitPush(ILGen gen) { 
			opAction(operand1, operand2, gen);
		}
	}

	public class ILBitwiseBinaryOp : Expr {
		Expr operand1, operand2;

		Action<Expr, Expr, ILGen> opAction;

		public override Type Type { get; }

		public ILBitwiseBinaryOp(Expr operand1, Expr operand2, Action<Expr, Expr, ILGen> opAction) {
			this.opAction = opAction;
			NativeTypeInfo outputType = NumericOps.GetBitwiseOpResultType(operand1.Type, operand2.Type);
			if(outputType == null)
				throw new InSharpException("Not available ariphmetical operands \"{0}\" & \"{1}\"", operand1, operand2);
			Type = outputType.Type;

			this.operand1 = operand1.CompatiblePass(Type);
			this.operand2 = operand2.CompatiblePass(Type);
		}

		public override void emitPush(ILGen gen) { 
			opAction(operand1, operand2, gen);
		}
	}

	public class ILBitShiftBinaryOp : Expr {
		Expr operand1, operand2;
		bool rightSide;

		public override Type Type { get; }

		public ILBitShiftBinaryOp(Expr operand1, Expr operand2, bool rightSide) {
			NativeTypeInfo outputType = NumericOps.GetBitwiseOpResultType(operand1.Type, operand2.Type);
			if(outputType == null)
				throw new InSharpException("Not available ariphmetical operands \"{0}\" & \"{1}\"", operand1, operand2);
			Type = outputType.Type;

			this.operand1 = operand1.CompatiblePass(Type);
			this.operand2 = operand2.CompatiblePass(Type);
			this.rightSide = rightSide;
		}

		public override void emitPush(ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			int op2Mask = NumericOps.GetTypeInfoByType(operand2.Type).BytesSize == 8 ? 63 : 31;
			gen.il.Emit(OpCodes.Ldc_I4_S, op2Mask);
			gen.OutDebug("OpCodes.Ldc_I4_S, {0}", op2Mask);
			gen.il.Emit(OpCodes.And);
			gen.OutDebug("OpCodes.And");
			if(rightSide) {
				gen.il.Emit(OpCodes.Shr);
				gen.OutDebug("OpCodes.Shr");
			} else { 
				gen.il.Emit(OpCodes.Shl);
				gen.OutDebug("OpCodes.Shl");
			}
		}
	}

	public class ILRelationBinaryOp : Expr {
		Expr operand1, operand2;

		Action<Expr, Expr, ILGen> opAction;

		public override Type Type { get; }

		public ILRelationBinaryOp(Expr operand1, Expr operand2, Action<Expr, Expr, ILGen> opAction) {
			this.opAction = opAction;
			NativeTypeInfo commonType = NumericOps.GetBitwiseOpResultType(operand1.Type, operand2.Type);
			if(commonType == null)
				throw new InSharpException("Not available ariphmetical operands \"{0}\" & \"{1}\"", operand1, operand2);
			Type = typeof(bool);

			this.operand1 = operand1.CompatiblePass(commonType.Type);
			this.operand2 = operand2.CompatiblePass(commonType.Type);
		}

		public override void emitPush(ILGen gen) { 
			opAction(operand1, operand2, gen);
		}
	}

	public class ILUnaryOp : Expr {
		Expr operand1;

		Action<Expr, ILGen> opAction;

		public override Type Type { get; }

		public ILUnaryOp(Expr operand1, Action<Expr, ILGen> opAction) {
			this.opAction = opAction;
			Type = operand1.Type;
		}

		public override void emitPush(ILGen gen) { 
			opAction(operand1, gen);
		}
	}


	public static partial class Ops {

		/*--------------------------------Arithmetical--------------------------------*/

		private static Expr AddOperationFactory(Expr op1, Expr op2) { return new ILArithmeticBinaryOp(op1, op2, OperationAdd); }
		private static void OperationAdd(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Add);
			gen.OutDebug("OpCodes.Add");
		}

		private static Expr SubOperationFactory(Expr op1, Expr op2) { return new ILArithmeticBinaryOp(op1, op2, OperationSub); }
		private static void OperationSub(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Sub);
			gen.OutDebug("OpCodes.Sub");
		}

		private static Expr MulOperationFactory(Expr op1, Expr op2) { return new ILArithmeticBinaryOp(op1, op2, OperationMul); }
		private static void OperationMul(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Mul);
			gen.OutDebug("OpCodes.Mul");
		}

		private static Expr DivOperationFactory(Expr op1, Expr op2) { return new ILArithmeticBinaryOp(op1, op2, OperationDiv); }
		private static void OperationDiv(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			if(NumericOps.IsUnsignedOp(operand1.Type, operand2.Type)) {
				gen.il.Emit(OpCodes.Div_Un);
				gen.OutDebug("OpCodes.Div_Un");
			} else { 
				gen.il.Emit(OpCodes.Div);
				gen.OutDebug("OpCodes.Div");
			}
		}

		private static Expr ModOperationFactory(Expr op1, Expr op2) { return new ILArithmeticBinaryOp(op1, op2, OperationMod); }
		private static void OperationMod(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			if(NumericOps.IsUnsignedOp(operand1.Type, operand2.Type)) { 
				gen.il.Emit(OpCodes.Rem_Un);
				gen.OutDebug("OpCodes.Rem_Un");
			} else { 
				gen.il.Emit(OpCodes.Rem);
				gen.OutDebug("OpCodes.Rem");
			}
		}


		/*--------------------------------Bitwise--------------------------------*/
		private static Expr AndOperationFactory(Expr op1, Expr op2) { return new ILBitwiseBinaryOp(op1, op2, OperationAnd); }
		private static void OperationAnd(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.And);
			gen.OutDebug("OpCodes.And");
		}

		private static Expr OrOperationFactory(Expr op1, Expr op2) { return new ILBitwiseBinaryOp(op1, op2, OperationOr); }
		private static void OperationOr(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Or);
			gen.OutDebug("OpCodes.Or");
		}

		private static Expr XOrOperationFactory(Expr op1, Expr op2) { return new ILBitwiseBinaryOp(op1, op2, OperationXOr); }
		private static void OperationXOr(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Xor);
			gen.OutDebug("OpCodes.Xor");
		}
		
		private static Expr RightShiftOperationFactory(Expr op1, Expr op2) { return new ILBitShiftBinaryOp(op1, op2, true); }
		private static Expr LeftShiftOperationFactory(Expr op1, Expr op2) { return new ILBitShiftBinaryOp(op1, op2, false); }


		/*--------------------------------Relation--------------------------------*/
		private static Expr EqualOperationFactory(Expr op1, Expr op2) { return new ILRelationBinaryOp(op1, op2, OperationEqual); }
		private static void OperationEqual(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Ceq);
			gen.OutDebug("OpCodes.Ceq");
		}

		private static Expr LessOperationFactory(Expr op1, Expr op2) { return new ILRelationBinaryOp(op1, op2, OperationLess); }
		private static void OperationLess(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Clt);
			gen.OutDebug("OpCodes.Clt");
		}

		private static Expr GreaterOperationFactory(Expr op1, Expr op2) { return new ILRelationBinaryOp(op1, op2, OperationGreater); }
		private static void OperationGreater(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Cgt);
			gen.OutDebug("OpCodes.Cgt");
		}

		private static Expr NotEqualOperationFactory(Expr op1, Expr op2) { return new ILRelationBinaryOp(op1, op2, OperationNotEqual); }
		private static void OperationNotEqual(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Ceq);
			gen.OutDebug("OpCodes.Ceq");
			gen.il.Emit(OpCodes.Ldc_I4_0);
			gen.OutDebug("OpCodes.Ldc_I4_0");
			gen.il.Emit(OpCodes.Ceq);
			gen.OutDebug("OpCodes.Ceq");
		}

		private static Expr GEqualOperationFactory(Expr op1, Expr op2) { return new ILRelationBinaryOp(op1, op2, OperationGEqual); }
		private static void OperationGEqual(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Clt);
			gen.OutDebug("OpCodes.Clt");
			gen.il.Emit(OpCodes.Ldc_I4_0);
			gen.OutDebug("OpCodes.Ldc_I4_0");
			gen.il.Emit(OpCodes.Ceq);
			gen.OutDebug("OpCodes.Ceq");
		}

		private static Expr LEqualOperationFactory(Expr op1, Expr op2) { return new ILRelationBinaryOp(op1, op2, OperationLEqual); }
		private static void OperationLEqual(Expr operand1, Expr operand2, ILGen gen) { 
			operand1.emitPush(gen);
			operand2.emitPush(gen);
			gen.il.Emit(OpCodes.Cgt);
			gen.OutDebug("OpCodes.Cgt");
			gen.il.Emit(OpCodes.Ldc_I4_0);
			gen.OutDebug("OpCodes.Ldc_I4_0");
			gen.il.Emit(OpCodes.Ceq);
			gen.OutDebug("OpCodes.Ceq");
		}

		/*--------------------------------Unary operations--------------------------------*/
		private static Expr NotOperationFactory(Expr op1) { return new ILUnaryOp(op1, OperationNot); }
		private static void OperationNot(Expr operand1, ILGen gen) { 
			operand1.emitPush(gen);
			gen.il.Emit(OpCodes.Ldc_I4_0);
			gen.OutDebug("OpCodes.Ldc_I4_0");
			gen.il.Emit(OpCodes.Ceq);
			gen.OutDebug("OpCodes.Ceq");
		}

		private static Expr InverseOperationFactory(Expr op1) { return new ILUnaryOp(op1, OperationInv); }
		private static void OperationInv(Expr operand1, ILGen gen) { 
			operand1.emitPush(gen);
			gen.il.Emit(OpCodes.Not);
			gen.OutDebug("OpCodes.Not");
		}

	}


}
