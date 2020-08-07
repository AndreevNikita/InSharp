using LowLevelOpsHelper;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ShowCode_Casts {

	class A1Class { 

		
	}

	class A2Class : A1Class { 
		public static int operator+(A2Class arg1, B2Class arg2) { 
			return 0;
		}

		public static A2Class operator++(A2Class arg1) { 
			return null;
		}
		
	}

	class A3Class : A2Class { 
	}

	class B1Class {
		public static int operator+(A3Class arg1, B1Class arg2) { 
			return 0;
		}

		public int this[int a, params int[] b] { 
			get => 0;
		}

		public int this[int a] { 
			get => 0;
		}
	}

	class B2Class : B1Class { 
		public static implicit operator A2Class(B2Class _) { return null; }
	}

	class B3Class : B2Class { 
	}

	class CastTest {
		public static void Test() {
			//var i = new A3Class() + new B3Class();
			foreach(var parameterInfo in typeof(B1Class).GetProperties().Where((pinfo) => pinfo.GetIndexParameters().Length > 0))
				Console.WriteLine(parameterInfo);
			Console.WriteLine(typeof(B1Class).GetProperty("Item",  new[] { typeof(int), typeof(int).MakeArrayType() }));
			Console.WriteLine(ClassesOps.OPERATION_INC.FindMethod(typeof(A2Class)));
		}
	}
}
