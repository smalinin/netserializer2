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
using System.Runtime.Serialization;

namespace NetSerializer2
{
	static class DeserializerCodegen
	{
		public static DynamicMethod GenerateDynamicDeserializerStub(Type type)
		{
			var dm = new DynamicMethod("Deserialize", null,
				new Type[] { typeof(Serializer), typeof(Stream), type.MakeByRefType(), typeof(ObjectList) },
				typeof(Serializer), true);
			dm.DefineParameter(1, ParameterAttributes.None, "serializer");
			dm.DefineParameter(2, ParameterAttributes.None, "stream");
			dm.DefineParameter(3, ParameterAttributes.Out, "value");
			dm.DefineParameter(4, ParameterAttributes.None, "objList");

			return dm;
		}

		public static DynamicMethod GenerateDynamicDeserializeInvokerStub()
		{
			var dm = new DynamicMethod(string.Empty, null,
				new Type[] { typeof(Serializer), typeof(Stream), typeof(object).MakeByRefType(), typeof(ObjectList) },
				typeof(Serializer), true);
			dm.DefineParameter(1, ParameterAttributes.None, "serializer");
			dm.DefineParameter(2, ParameterAttributes.None, "stream");
			dm.DefineParameter(3, ParameterAttributes.Out, "value");
			dm.DefineParameter(4, ParameterAttributes.None, "objList");

			return dm;
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		public static MethodBuilder GenerateStaticDeserializerStub(TypeBuilder tb, Type type)
		{
			var mb = tb.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Static, null,
						new Type[] { typeof(Serializer), typeof(Stream), type.MakeByRefType(), typeof(ObjectList) });
			mb.DefineParameter(1, ParameterAttributes.None, "serializer");
			mb.DefineParameter(2, ParameterAttributes.None, "stream");
			mb.DefineParameter(3, ParameterAttributes.Out, "value");
			mb.DefineParameter(4, ParameterAttributes.None, "objList");
			return mb;
		}

		public static MethodBuilder GenerateStaticDeserializeInvokerStub(TypeBuilder tb, int typeID)
		{
			var mb = tb.DefineMethod("DeserializeInv" + typeID, MethodAttributes.Public | MethodAttributes.Static, null,
						new Type[] { typeof(Serializer), typeof(Stream), typeof(object).MakeByRefType(), typeof(ObjectList) });
			mb.DefineParameter(1, ParameterAttributes.None, "serializer");
			mb.DefineParameter(2, ParameterAttributes.None, "stream");
			mb.DefineParameter(3, ParameterAttributes.Out, "value");
			mb.DefineParameter(4, ParameterAttributes.None, "objList");
			return mb;
		}
#endif

		public static void GenerateDeserializerBody(CodeGenContext ctx, Type type, ILGenerator il)
		{
			// arg0: stream, arg1: out value
			//--			D(il, "deser {0}", type.Name);

			if (type.IsArray)
				GenDeserializerBodyForArray(ctx, type, il);
			else
				GenDeserializerBody(ctx, type, il);
		}


		static void GenDeserializerBody(CodeGenContext ctx, Type type, ILGenerator il)
		{
			if (type.IsClass)
			{
				// instantiate empty class
				il.Emit(OpCodes.Ldarg_2);

				var gtfh = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
				var guo = typeof(System.Runtime.Serialization.FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);
				il.Emit(OpCodes.Ldtoken, type);
				il.Emit(OpCodes.Call, gtfh);
				il.Emit(OpCodes.Call, guo);
				il.Emit(OpCodes.Castclass, type);

				il.Emit(OpCodes.Stind_Ref);

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
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_2);
				if (type.IsClass)
					il.Emit(OpCodes.Ldind_Ref);
				il.Emit(OpCodes.Ldflda, field);
				il.Emit(OpCodes.Ldarg_3);

				GenDeserializerCall(ctx, il, field.FieldType);
			}

			if (typeof(IDeserializationCallback).IsAssignableFrom(type))
			{
				var miOnDeserialization = typeof(IDeserializationCallback).GetMethod("OnDeserialization",
										BindingFlags.Instance | BindingFlags.Public,
										null, new[] { typeof(Object) }, null);

				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Constrained, type);
				il.Emit(OpCodes.Callvirt, miOnDeserialization);
			}

			il.Emit(OpCodes.Ret);
		}

		static void GenDeserializerBodyForArray(CodeGenContext ctx, Type type, ILGenerator il)
		{
			var elemType = type.GetElementType();

			var lenLocal = il.DeclareLocal(typeof(uint));

			// read array len
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldloca_S, lenLocal);
			il.Emit(OpCodes.Ldarg_3);
			il.EmitCall(OpCodes.Call, ctx.GetReaderMethodInfo(typeof(uint)), null);

			var notNullLabel = il.DefineLabel();

			/* if len == 0, return null */
			il.Emit(OpCodes.Ldloc_S, lenLocal);
			il.Emit(OpCodes.Brtrue_S, notNullLabel);

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stind_Ref);
			il.Emit(OpCodes.Ret);

			il.MarkLabel(notNullLabel);

			var arrLocal = il.DeclareLocal(type);

			// create new array with len - 1
			il.Emit(OpCodes.Ldloc_S, lenLocal);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Sub);
			il.Emit(OpCodes.Newarr, elemType);
			il.Emit(OpCodes.Stloc_S, arrLocal);

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

			// read element to arr[i]
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldloc_S, arrLocal);
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldelema, elemType);
			il.Emit(OpCodes.Ldarg_3);

			GenDeserializerCall(ctx, il, elemType);

			// i = i + 1
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Stloc_S, idxLocal);

			il.MarkLabel(loopCheckLabel);

			// loop condition
			il.Emit(OpCodes.Ldloc_S, idxLocal);
			il.Emit(OpCodes.Ldloc_S, arrLocal);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Clt);
			il.Emit(OpCodes.Brtrue_S, loopBodyLabel);


			// store new array to the out value
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldloc_S, arrLocal);
			il.Emit(OpCodes.Stind_Ref);


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

			il.Emit(OpCodes.Ret);
		}

		static void GenDeserializerCall(CodeGenContext ctx, ILGenerator il, Type type)
		{
			// We can call the Deserializer method directly for:
			// - Value types
			// - Array types
			// - Sealed types with static Deserializer method, as the method will handle null
			// Other reference types go through the DeserializesSwitch

			bool direct;

			if (type.IsValueType || type.IsArray)
				direct = true;
			else if (type.IsSealed && ctx.IsDynamic(type) == false)
				direct = true;
			else
				direct = false;

			var method = direct ? ctx.GetReaderMethodInfo(type) : typeof(NetSerializer2.Serializer).GetMethod("_DeserializerSwitch");

			il.EmitCall(OpCodes.Call, method, null);
		}

	}
}
