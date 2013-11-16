/*
 *  Copyright (c) 2013 Sergey Malinin
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 *  --------------------------------------------------------------------
 *
 *  This package is based on NetSerializer (license MPL v. 2.0), 
 *  originally developed by Tomi Valkeinen.
 *  https://github.com/tomba/netserializer
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace NetSerializer2
{
	static partial class SerializerCodegen
	{
		public static DynamicMethod GenerateDynamicSerializerStub(Type type)
		{
			var dm = new DynamicMethod("Serialize", null,
				new Type[] { typeof(Serializer), typeof(Stream), type, typeof(ObjectList) },
				typeof(Serializer), true);

			dm.DefineParameter(1, ParameterAttributes.None, "serializer");
			dm.DefineParameter(2, ParameterAttributes.None, "stream");
			dm.DefineParameter(3, ParameterAttributes.None, "value");
			dm.DefineParameter(4, ParameterAttributes.None, "objList");

			return dm;
		}

		public static DynamicMethod GenerateDynamicSerializeInvokerStub()
		{
			var dm = new DynamicMethod(string.Empty, null,
				new Type[] { typeof(Serializer), typeof(Stream), typeof(object), typeof(ObjectList) },
				typeof(Serializer), true);

			dm.DefineParameter(1, ParameterAttributes.None, "serializer");
			dm.DefineParameter(2, ParameterAttributes.None, "stream");
			dm.DefineParameter(3, ParameterAttributes.None, "value");
			dm.DefineParameter(4, ParameterAttributes.None, "objList");

			return dm;
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		public static MethodBuilder GenerateStaticSerializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Static, null,
						new Type[] { typeof(Serializer), typeof(Stream), type, typeof(ObjectList) });
			mb.DefineParameter(1, ParameterAttributes.None, "serializer");
			mb.DefineParameter(2, ParameterAttributes.None, "stream");
			mb.DefineParameter(3, ParameterAttributes.None, "value");
			mb.DefineParameter(4, ParameterAttributes.None, "objList");
			return mb;
		}

		public static MethodBuilder GenerateStaticSerializeInvokerStub(TypeBuilder tb, int typeID)
		{
			var mb = tb.DefineMethod("SerializeInv" + typeID,	MethodAttributes.Public | MethodAttributes.Static, null,
						new Type[] { typeof(Serializer), typeof(Stream), typeof(object), typeof(ObjectList) });
			mb.DefineParameter(1, ParameterAttributes.None, "serializer");
			mb.DefineParameter(2, ParameterAttributes.None, "stream");
			mb.DefineParameter(3, ParameterAttributes.None, "value");
			mb.DefineParameter(4, ParameterAttributes.None, "objList");
			return mb;
		}
#endif

		public static void GenerateSerializerBody(CodeGenContext ctx, Type type, ILGenerator il)
		{
			// arg0: Stream, arg1: value
			//--			D(il, "ser {0}", type.Name);

			if (type.IsArray)
				GenSerializerBodyForArray(ctx, type, il);
			else
				GenSerializerBody(ctx, type, il);
		}

		static void GenSerializerBody(CodeGenContext ctx, Type type, ILGenerator il)
		{
			if (type.IsClass)
			{
				Type objStackType = typeof(ObjectList);
				MethodInfo getAddMethod = objStackType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(object) }, null);

				var endLabel = il.DefineLabel();

				//==if(objList==null)  goto endLabel; 
				il.Emit(OpCodes.Ldarg_3);
				il.Emit(OpCodes.Brfalse_S, endLabel);

				//== objList.Add(value);
				il.Emit(OpCodes.Ldarg_3);
				il.Emit(OpCodes.Ldarg_2);
				il.EmitCall(OpCodes.Call, getAddMethod, null);

				il.MarkLabel(endLabel);
			}

			var fields = Helpers.GetFieldInfos(type);

			foreach (var field in fields)
			{
				// Note: the user defined value type is not passed as reference. could cause perf problems with big structs

				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				if (type.IsValueType)
					il.Emit(OpCodes.Ldarga_S, 2);
				else
					il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldfld, field);
				il.Emit(OpCodes.Ldarg_3);

				GenSerializerCall(ctx, il, field.FieldType);
			}

			il.Emit(OpCodes.Ret);
		}

		static void GenSerializerBodyForArray(CodeGenContext ctx, Type type, ILGenerator il)
		{
			var elemType = type.GetElementType();

			var notNullLabel = il.DefineLabel();

			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Brtrue_S, notNullLabel);

			// if value == null, write 0
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Ldarg_3);
			il.EmitCall(OpCodes.Call, ctx.GetWriterMethodInfo(typeof(uint)), null);
			il.Emit(OpCodes.Ret);

			il.MarkLabel(notNullLabel);

			//==============
			Type objStackType = typeof(ObjectList);
			MethodInfo getAddMethod = objStackType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(object) }, null);

			var endLabel = il.DefineLabel();

			//==if(objList==null)  goto endLabel; 
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Brfalse_S, endLabel);

			//== objList.Add(value);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Ldarg_2);
			il.EmitCall(OpCodes.Call, getAddMethod, null);

			il.MarkLabel(endLabel);

			//==============
			// write array len + 1
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Ldarg_3);
			il.EmitCall(OpCodes.Call, ctx.GetWriterMethodInfo(typeof(uint)), null);

			// declare i
			var idxLocal = il.DeclareLocal(typeof(int));

			// i = 0
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stloc_S, idxLocal);

			var loopBodyLabel = il.DefineLabel();
			var loopCheckLabel = il.DefineLabel();

			il.Emit(OpCodes.Br_S, loopCheckLabel);

			// loop body
			il.MarkLabel(loopBodyLabel);

			// write element at index i
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldelem, elemType);
			il.Emit(OpCodes.Ldarg_3);

			GenSerializerCall(ctx, il, elemType);

			// i = i + 1
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Stloc_S, idxLocal);

			il.MarkLabel(loopCheckLabel);

			// loop condition
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Clt);
			il.Emit(OpCodes.Brtrue_S, loopBodyLabel);

			il.Emit(OpCodes.Ret);
		}

		static void GenSerializerCall(CodeGenContext ctx, ILGenerator il, Type type)
		{
			// We can call the Serializer method directly for:
			// - Value types
			// - Array types
			// - Sealed types with static Serializer method, as the method will handle null
			// Other reference types go through the SerializesSwitch

			bool direct;

			if (type.IsValueType || type.IsArray)
				direct = true;
			else if (type.IsSealed && ctx.IsDynamic(type) == false)
				direct = true;
			else
				direct = false;

			var method = direct ? ctx.GetWriterMethodInfo(type) : typeof(NetSerializer2.Serializer).GetMethod("_SerializerSwitch");

			il.EmitCall(OpCodes.Call, method, null);
		}

	}
}
