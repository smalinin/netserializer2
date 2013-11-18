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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;


namespace NetSerializer2
{
	public partial class Serializer
	{
		class SerializationID
		{
			static internal Dictionary<Type, uint> predefinedID;
			internal const uint typeIDstart = 96;

			static SerializationID()
			{
				predefinedID = new Dictionary<Type, uint>();
				// TypeID 0 is reserved for null
				predefinedID.Add(typeof(ObjectRef), 1);

				predefinedID.Add(typeof(ClassFieldInfo), 2);
				predefinedID.Add(typeof(ClassInfo), 3);
				predefinedID.Add(typeof(TypeData), 4);
				predefinedID.Add(typeof(List<TypeData>), 5);
				predefinedID.Add(typeof(List<ClassFieldInfo>), 6);

				predefinedID.Add(typeof(bool), 7);
				predefinedID.Add(typeof(bool?), 8);
				predefinedID.Add(typeof(byte), 9);
				predefinedID.Add(typeof(byte?), 10);
				predefinedID.Add(typeof(sbyte), 11);
				predefinedID.Add(typeof(sbyte?), 12);
				predefinedID.Add(typeof(char), 13);
				predefinedID.Add(typeof(char?), 14);
				predefinedID.Add(typeof(ushort), 15);
				predefinedID.Add(typeof(ushort?), 16);
				predefinedID.Add(typeof(short), 17);
				predefinedID.Add(typeof(short?), 18);
				predefinedID.Add(typeof(uint), 19);
				predefinedID.Add(typeof(uint?), 20);
				predefinedID.Add(typeof(int), 21);
				predefinedID.Add(typeof(int?), 22);
				predefinedID.Add(typeof(ulong), 23);
				predefinedID.Add(typeof(ulong?), 24);
				predefinedID.Add(typeof(long), 25);
				predefinedID.Add(typeof(long?), 26);
				predefinedID.Add(typeof(float), 27);
				predefinedID.Add(typeof(float?), 28);
				predefinedID.Add(typeof(double), 29);
				predefinedID.Add(typeof(double?), 30);

				predefinedID.Add(typeof(string), 31);
				predefinedID.Add(typeof(DateTime), 32);
				predefinedID.Add(typeof(object), 33);

				predefinedID.Add(typeof(TimeSpan), 34);
				predefinedID.Add(typeof(DateTimeOffset), 35);
				predefinedID.Add(typeof(decimal), 36);
				predefinedID.Add(typeof(Guid), 37);

				predefinedID.Add(typeof(bool[]), 38);
				predefinedID.Add(typeof(byte[]), 39);
				predefinedID.Add(typeof(sbyte[]), 40);
				predefinedID.Add(typeof(char[]), 41);
				predefinedID.Add(typeof(ushort[]), 42);
				predefinedID.Add(typeof(short[]), 43);
				predefinedID.Add(typeof(uint[]), 44);
				predefinedID.Add(typeof(int[]), 45);
				predefinedID.Add(typeof(ulong[]), 46);
				predefinedID.Add(typeof(long[]), 47);
				predefinedID.Add(typeof(float[]), 48);
				predefinedID.Add(typeof(double[]), 49);
				predefinedID.Add(typeof(string[]), 50);
				predefinedID.Add(typeof(DateTime[]), 51);
				predefinedID.Add(typeof(object[]), 52);
				predefinedID.Add(typeof(TimeSpan[]), 53);
				predefinedID.Add(typeof(DateTimeOffset[]), 54);
				predefinedID.Add(typeof(decimal[]), 55);
				predefinedID.Add(typeof(Guid[]), 56);


				predefinedID.Add(typeof(ArrayList), 57);
				predefinedID.Add(typeof(BitArray), 58);
				predefinedID.Add(typeof(Hashtable), 59);
				predefinedID.Add(typeof(Queue), 60);
				predefinedID.Add(typeof(Stack), 61);
				predefinedID.Add(typeof(SortedList), 62);

			}
		}



		public delegate void SerializationInvokeHandler(Serializer serializer, Stream stream, object val, ObjectList objList);
		public delegate void DeserializationInvokeHandler(Serializer serializer, Stream stream, out object val, ObjectList objList);

		private static DeserializationInvokeHandler GetDeserializationInvoker(TypeBuilder tb, MethodInfo methodInfo, Type val_type,  int typeID)
		{
			DynamicMethod dynamicMethod = null;
			ILGenerator il;
#if GENERATE_DEBUGGING_ASSEMBLY
			if (tb != null)
			{
				var methodBuilder = DeserializerCodegen.GenerateStaticDeserializeInvokerStub(tb, typeID);
				il = methodBuilder.GetILGenerator();
			}
			else
#endif
			{
				dynamicMethod = DeserializerCodegen.GenerateDynamicDeserializeInvokerStub();
				il = dynamicMethod.GetILGenerator();
			}

			var local = il.DeclareLocal(val_type);

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldloca_S, local);
			il.Emit(OpCodes.Ldarg_3);

			if (methodInfo.IsGenericMethodDefinition)
			{
				Debug.Assert(val_type.IsGenericType);
				var genArgs = val_type.GetGenericArguments();
				il.EmitCall(OpCodes.Call, methodInfo.MakeGenericMethod(genArgs), null);
			}
			else
			{
				il.EmitCall(OpCodes.Call, methodInfo, null);
			}

			// write result object to out object
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldloc_S, local);

			if (val_type.IsValueType)
				il.Emit(OpCodes.Box, val_type);
			il.Emit(OpCodes.Stind_Ref);
			il.Emit(OpCodes.Ret);

			if (tb != null)
				return null;
			else
				return (DeserializationInvokeHandler)dynamicMethod.CreateDelegate(typeof(DeserializationInvokeHandler));
		}

		private static SerializationInvokeHandler GetSerializationInvoker(TypeBuilder tb, MethodInfo methodInfo, Type val_type, int typeID)
		{
			DynamicMethod dynamicMethod = null;
			ILGenerator il;
#if GENERATE_DEBUGGING_ASSEMBLY
			if (tb != null)
			{
				var methodBuilder = SerializerCodegen.GenerateStaticSerializeInvokerStub(tb, typeID);
				il = methodBuilder.GetILGenerator();
			}
			else
#endif
			{
				dynamicMethod = SerializerCodegen.GenerateDynamicSerializeInvokerStub();
				il = dynamicMethod.GetILGenerator();
			}

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(val_type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, val_type);
			il.Emit(OpCodes.Ldarg_3);

			if (methodInfo.IsGenericMethodDefinition)
			{
				Debug.Assert(val_type.IsGenericType);
				var genArgs = val_type.GetGenericArguments();
				il.EmitCall(OpCodes.Call, methodInfo.MakeGenericMethod(genArgs), null);
			}
			else
			{
				il.EmitCall(OpCodes.Call, methodInfo, null);
			}

			il.Emit(OpCodes.Ret);

			if (tb != null)
				return null;
			else
				return (SerializationInvokeHandler)dynamicMethod.CreateDelegate(typeof(SerializationInvokeHandler));
		}

		// Global TypeData cache 
		private static ConcurrentDictionary<Type, TypeData> g_type_TypeData = new ConcurrentDictionary<Type, TypeData>();
		private SimpleRwLock m_lck;

		private List<TypeData>  m_registredTypeData = new List<TypeData>(128); 
		private Dictionary<uint, TypeData> m_typeID_TypeData = new Dictionary<uint, TypeData>(128);
		private Dictionary<Type, TypeData> m_type_TypeData = new Dictionary<Type, TypeData>(128);
		private uint typeID = SerializationID.typeIDstart;
		private bool s_initialized;

		private bool m_autoRegister = false;
		private bool m_autoAssignObjID = false;

		public Serializer SetAutoRegister(bool val)
		{
			m_autoRegister = val;
			if (val) // autoRegister required autoAssignObjID
				m_autoAssignObjID = true;
			return this;
		}

		public Serializer SetAutoAssignObjID(bool val)
		{
			m_autoAssignObjID = val;
			return this;
		}

		public bool AutoRegister
		{
			get { return m_autoRegister; }
			set
			{
				m_autoRegister = value;
				if (value) // autoRegister required autoAssignObjID
					m_autoAssignObjID = true;
			}
		}

		public bool AutoAssignObjID
		{
			get { return m_autoAssignObjID; }
			set
			{
				if (m_autoRegister && !value)
					throw new InvalidOperationException("NetSerializer2 could not set AutoAssignObjID to false, if AutoRegister==true .");
				m_autoAssignObjID = value;
			}
		}

		public Serializer Initialize()
		{
			Initialize((Type[])null);
			return this;
		}

		public Serializer Initialize(Type[] rootTypes)
		{
			if (s_initialized)
				throw new InvalidOperationException("NetSerializer2 already initialized");

			var types = CollectTypes(rootTypes);

#if GENERATE_DEBUGGING_ASSEMBLY
			GenerateAssembly(types, null);
#endif
			var map_Type2TypeData = GenerateDynamic(types, null);
			foreach (var kv in map_Type2TypeData)
			{
				m_type_TypeData.Add(kv.Key, kv.Value);
				m_typeID_TypeData.Add(kv.Value.TypeID, kv.Value);
				if (kv.Value.TypeID >= SerializationID.typeIDstart)
					m_registredTypeData.Add(kv.Value);
			}

			s_initialized = true;
			return this;
		}

		public Serializer Initialize(Stream ins)
		{
			if (s_initialized)
				throw new InvalidOperationException("NetSerializer2 already initialized");

			Initialize((Type[])null);

			var ver = (uint) Deserialize(ins);
			var lastTypeID = (uint) Deserialize(ins);
			var listTypeData = (List<TypeData>) Deserialize(ins);

			if (ver != 0)
				throw new IOException("NetSerializer2 wrong version number was found");

			var map_ID_TypeData = listTypeData.ToDictionary(kvp => kvp.TypeID, kvp => kvp);
			var map_Type_ID = new Dictionary<Type, uint>();
			foreach (var i in listTypeData)
			{
				var i_type = Type.GetType(i.TypeInfo.TypeName, false);
				if (i_type!=null)
					map_Type_ID.Add(i_type, i.TypeID);
			}

            //Compare loaded Types and existed Types
			foreach (var t in map_Type_ID)
			{
				Type t_type = t.Key;
				var t_typeInfo = map_ID_TypeData[t.Value].TypeInfo;
				int i = 0;
				foreach (var cur_f in Helpers.GetFieldInfos(t_type))
				{
					if (i >= t_typeInfo.Fields.Count)
						throw new IOException(String.Format("The Count of fields for stored and current Type: '{0}' aren't equal. Old:'{1}' New:'{2}'", t_type.Name, t_typeInfo.Fields.Count, i));
					var l_fld = t_typeInfo.Fields[i];
					if (cur_f.Name != l_fld.FieldName)
						throw new IOException(String.Format("Type:'{0}'. FieldName was changed in pos:{1}. Old:'{2}' New:'{3}'", t_type.FullName, i, l_fld.FieldName, cur_f.Name));
					if (Helpers.GetTypeName(cur_f.FieldType) != l_fld.TypeName)
						throw new IOException(String.Format("Type:'{0}'. Type for Fieled:'{1}' was changed. Old:'{2}' New:'{3}'", t_type.FullName, cur_f.Name, l_fld.TypeName, Helpers.GetTypeName(cur_f.FieldType)));
					i++;
				}
				if (i != t_typeInfo.Fields.Count)
					throw new IOException(String.Format("The Count of fields for stored and current Type: '{0}' aren't equal. Old:'{1}' New:'{2}'", t_type.Name, t_typeInfo.Fields.Count, i));
			}

			var types = map_Type_ID.Select(v => v.Key).ToArray<Type>();

			typeID = lastTypeID;
#if GENERATE_DEBUGGING_ASSEMBLY
			GenerateAssembly(types, map_Type_ID);
#endif
			var map_Type2TypeData = GenerateDynamic(types, map_Type_ID);
			foreach (var kv in map_Type2TypeData)
			{
				m_type_TypeData.Add(kv.Key, kv.Value);
				m_typeID_TypeData.Add(kv.Value.TypeID, kv.Value);
				if (kv.Value.TypeID >= SerializationID.typeIDstart)
					m_registredTypeData.Add(kv.Value);
			}
			m_registredTypeData.AddRange(listTypeData);

			return this;
		}

		public void SaveState(Stream outs)
		{
			List<TypeData> listTypeData = null;
			uint lastTypeID = 0;
			uint ver = 0;

#if NEW_LCK
			m_lck.EnterReadLock();
			try
#else
			lock (this)
#endif
			{
				lastTypeID = typeID;
				listTypeData = m_registredTypeData.ToList<TypeData>();
			}
#if NEW_LCK
			finally
			{
				m_lck.ExitReadLock();
			}
#endif
			Serialize(outs, ver);
			Serialize(outs, lastTypeID);
			Serialize(outs, listTypeData);
		}

		public void Register(Type[] regTypes)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer2 not initialized");
			if (!m_autoAssignObjID)
				throw new InvalidOperationException("NetSerializer2 couldn't generate ID for new objects with disabled AutoAssignObjID");

			var ctypes = CollectTypes(regTypes);
			var types = ctypes.Select(v => v).Where(v => !m_type_TypeData.ContainsKey(v)).ToArray<Type>();

#if NEW_LCK
			m_lck.EnterWriteLock();
			try
#else
			lock (this)
#endif
			{
				var map_Type2TypeData = GenerateDynamic(types, null);
				foreach (var kv in map_Type2TypeData)
				{
					m_type_TypeData.Add(kv.Key, kv.Value);
					m_typeID_TypeData.Add(kv.Value.TypeID, kv.Value);
					if (kv.Value.TypeID >= SerializationID.typeIDstart)
						m_registredTypeData.Add(kv.Value);
				}
			}
#if NEW_LCK
			finally
			{
				m_lck.ExitWriteLock();
			}
#endif
		}

		public int Register(Type regType, uint[] typeID)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer2 not initialized");

			if (!m_autoAssignObjID && typeID==null)
				throw new InvalidOperationException("NetSerializer2 couldn't generate ID for new objects with disabled AutoAssignObjID");
 
			var ctypes = CollectTypes(new Type[]{regType});
			var types = ctypes.Select(v => v).Where(v => !m_type_TypeData.ContainsKey(v)).ToArray<Type>();

			Dictionary<Type, uint> map_Type2id = null;
			if (!m_autoAssignObjID)
			{
				if (types.Length > typeID.Length)
					throw new InvalidOperationException("NetSerializer2: Type=" + regType.Name + " requires " + types.Length + " ID, but was received only " + typeID.Length);
				map_Type2id = new Dictionary<Type, uint>();
				int i = 0;
				foreach (var type in types)
					map_Type2id.Add(type, typeID[i++]);
			} 
#if NEW_LCK
			m_lck.EnterWriteLock();
			try
#else
			lock (this)
#endif
			{
				var map_Type2TypeData = GenerateDynamic(types, map_Type2id);
				foreach (var kv in map_Type2TypeData)
				{
					m_type_TypeData.Add(kv.Key, kv.Value);
					m_typeID_TypeData.Add(kv.Value.TypeID, kv.Value);
					if (kv.Value.TypeID >= SerializationID.typeIDstart)
						m_registredTypeData.Add(kv.Value);
				}
			}
#if NEW_LCK
			finally
			{
				m_lck.ExitWriteLock();
			}
#endif
			return types.Length;
		}

		public void SerializeDeep(Stream stream, object data)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer2 not initialized");

			Serialize(stream, data, new ObjectList());
		}

		public void Serialize(Stream stream, object data)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer2 not initialized");

			Serialize(stream, data, null);
		}

		internal void Serialize(Stream stream, object data, ObjectList objList)
		{
			D("Serializing {0}", data!=null?data.GetType().Name:"null");

			_SerializerSwitch(this, stream, data, objList);
		}

		public object Deserialize(Stream stream)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer2 not initialized");

			return Deserialize(stream, null);
		}

		public object DeserializeDeep(Stream stream)
		{
			if (!s_initialized)
				throw new InvalidOperationException("NetSerializer2 not initialized");

			return Deserialize(stream, new ObjectList());
		}

		internal object Deserialize(Stream stream, ObjectList objList)
		{
			D("Deserializing");

			object o;
			_DeserializerSwitch(this, stream, out o, objList);
			return o;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		static void D(string fmt, params object[] args)
		{
			//Console.WriteLine("S: " + String.Format(fmt, args));
		}

		[System.Diagnostics.Conditional("DEBUG")]
		static void D(ILGenerator ilGen, string fmt, params object[] args)
		{
			//ilGen.EmitWriteLine("E: " + String.Format(fmt, args));
		}

		void CollectTypes(Type type, HashSet<Type> typeSet)
		{
			if (typeSet.Contains(type))
				return;

			if (type.IsAbstract)
				return;

			if (type.IsInterface)
				return;

			if (!type.IsSerializable)
				throw new NotSupportedException(String.Format("Type {0} is not marked as Serializable", type.FullName));

			if (type.ContainsGenericParameters)
				throw new NotSupportedException(String.Format("Type {0} contains generic parameters", type.FullName));

			typeSet.Add(type);

			if (type.IsArray)
			{
				CollectTypes(type.GetElementType(), typeSet);
			}
			else if (type.IsGenericType)
			{
				var args = type.GetGenericArguments();
				Type genType = type.GetGenericTypeDefinition();
				if (args.Length == 2)
				{
					if (genType == typeof(Dictionary<,>)
							|| genType == typeof(ConcurrentDictionary<,>)
							|| genType == typeof(SortedDictionary<,>)
							|| genType == typeof(SortedList<,>)
							)
					{
						Debug.Assert(args.Length == 2);
#if OLD_DICT
						var keyValueType = typeof(KeyValuePair<,>).MakeGenericType(args);
						CollectTypes(keyValueType, typeSet);
#else
						var arrayType = typeof(KeyValuePair<,>).MakeGenericType(args).MakeArrayType();
						CollectTypes(arrayType, typeSet);
#endif
					}
				}
				else if (args.Length == 1 &&
						(genType == typeof(List<>)
					  || genType == typeof(ConcurrentQueue<>)
					  || genType == typeof(ConcurrentStack<>)
					  || genType == typeof(BlockingCollection<>)
					  || genType == typeof(Nullable<>)
					  || genType == typeof(HashSet<>)
					  || genType == typeof(LinkedList<>)
					  || genType == typeof(Queue<>)
					  || genType == typeof(SortedSet<>)
					  || genType == typeof(Stack<>)
					  || genType == typeof(ConcurrentBag<>)
//					  || genType == typeof(CopyOnWriteArrayList<>)
					))
				{
					Debug.Assert(args.Length == 1);
					CollectTypes(args[0], typeSet);
				}
				else
				{
				    var fields = Helpers.GetFieldInfos(type);

				    foreach (var field in fields)
					    CollectTypes(field.FieldType, typeSet);
				}
			}
			else
			{
				var fields = Helpers.GetFieldInfos(type);

				foreach (var field in fields)
					CollectTypes(field.FieldType, typeSet);
			}
		}


		Type[] CollectTypes(Type[] rootTypes)
		{
			var typeSet = s_initialized ? new HashSet<Type>() : new HashSet<Type>(SerializationID.predefinedID.Keys);

			if (rootTypes != null)
				foreach (var type in rootTypes)
					CollectTypes(type, typeSet);

			return typeSet
				.OrderBy(t => t.FullName, StringComparer.Ordinal)
				.ToArray();
		}

		Dictionary<Type, TypeData> GenerateTypeData(Type[] types, Dictionary<Type, uint> loaded_TypeMap)
		{
			var map = new Dictionary<Type, TypeData>(types.Length);

			// TypeID 0 is reserved for null
			foreach (var type in types)
			{
				uint typeID;
				if (loaded_TypeMap == null)
				{
					if (!SerializationID.predefinedID.TryGetValue(type, out typeID))
						typeID = this.typeID++;
				}
				else
				{
					if (!loaded_TypeMap.TryGetValue(type, out typeID))
					{
						if (!SerializationID.predefinedID.TryGetValue(type, out typeID))
							typeID = this.typeID++;
					}
				}

				MethodInfo writer;
				MethodInfo reader;

				bool isStatic = Helpers.GetPrimitives(typeof(Primitives), type, out writer, out reader);

				if (type.IsPrimitive && isStatic == false)
					throw new InvalidOperationException(String.Format("Missing primitive read/write methods for {0}", type.FullName));

				var td = new TypeData(typeID, type);
				TypeData _g_td;

				if (g_type_TypeData.TryGetValue(type, out _g_td))
				{
					td.CopyFrom(_g_td);
					td.IsInitialized = true;
				}
				else
				{
					if (isStatic)
					{
						if (writer.IsGenericMethodDefinition)
						{
							Debug.Assert(type.IsGenericType);
							var genArgs = type.GetGenericArguments();

							writer = writer.MakeGenericMethod(genArgs);
							reader = reader.MakeGenericMethod(genArgs);
						}

						td.WriterMethodInfo = writer;
						td.ReaderMethodInfo = reader;
						td.IsDynamic = false;
					}
					else
					{
						if (typeof(System.Runtime.Serialization.ISerializable).IsAssignableFrom(type))
							throw new InvalidOperationException(String.Format("Cannot serialize {0}: ISerializable not supported", type.FullName));

						td.IsDynamic = true;
						td.TypeInfo.IsDynamic = true;
					}
				}

				map[type] = td;
			}

			return map;
		}

		Dictionary<Type, TypeData> GenerateDynamic(Type[] types, Dictionary<Type, uint> loaded_TypeMap)
		{
			Dictionary<Type, TypeData> _map = GenerateTypeData(types, loaded_TypeMap); //new types

			/* generate stubs */
			foreach (var kv in _map.Where(kv => !kv.Value.IsInitialized).Where(kv => kv.Value.IsDynamic))
			{
				var s_dm = SerializerCodegen.GenerateDynamicSerializerStub(kv.Key);
				kv.Value.WriterMethodInfo = s_dm;
				kv.Value.WriterILGen = s_dm.GetILGenerator();

				var d_dm = DeserializerCodegen.GenerateDynamicDeserializerStub(kv.Key);
				kv.Value.ReaderMethodInfo = d_dm;
				kv.Value.ReaderILGen = d_dm.GetILGenerator();
			}

			var ctx = new CodeGenContext(m_type_TypeData.Concat(_map).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

			/* generate bodies */
			foreach (var kv in _map.Where(kv => !kv.Value.IsInitialized).Where(kv => kv.Value.IsDynamic))
			{
				SerializerCodegen.GenerateSerializerBody(ctx, kv.Key, kv.Value.WriterILGen);
				DeserializerCodegen.GenerateDeserializerBody(ctx, kv.Key, kv.Value.ReaderILGen);
				var fi = new List<ClassFieldInfo>();
				if (kv.Key.IsArray)
				{
					fi.Add(new ClassFieldInfo {TypeName = Helpers.GetTypeName(kv.Key.GetElementType()), SkipFieldAssign = false});
				}
				else
				{
					foreach (var f in Helpers.GetFieldInfos(kv.Key))
						fi.Add(new ClassFieldInfo
							{
								FieldName = f.Name,
								TypeName = Helpers.GetTypeName(f.FieldType),
								SkipFieldAssign = false
							});
				}
				kv.Value.TypeInfo.Fields = fi;
			}

			foreach (var kv in _map.Where(kv => !kv.Value.IsInitialized))
			{
				kv.Value.serializer = GetSerializationInvoker(null, kv.Value.WriterMethodInfo, kv.Key, (int)kv.Value.TypeID);
				kv.Value.deserializer = GetDeserializationInvoker(null, kv.Value.ReaderMethodInfo, kv.Key, (int)kv.Value.TypeID);

				g_type_TypeData.GetOrAdd(kv.Key, kv.Value);
			}

			return _map;
		}

#if GENERATE_DEBUGGING_ASSEMBLY
		void GenerateAssembly(Type[] types, Dictionary<Type, uint> loaded_TypeMap)
		{
			Dictionary<Type, TypeData> _map = GenerateTypeData(types, loaded_TypeMap);
			Dictionary<Type, TypeData> map = m_type_TypeData.Concat(_map).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

			var nonStaticTypes = map.Where(kvp => kvp.Value.IsDynamic).Select(kvp => kvp.Key);

			var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NetSerializer2Debug"), AssemblyBuilderAccess.RunAndSave);
			var modb = ab.DefineDynamicModule("NetSerializer2Debug.dll");
			var tb = modb.DefineType("NetSerializer2", TypeAttributes.Public);

			/* generate stubs */
			foreach (var type in nonStaticTypes)
			{
				var mb = SerializerCodegen.GenerateStaticSerializerStub(tb, type);
				map[type].WriterMethodInfo = mb;
				map[type].WriterILGen = mb.GetILGenerator();
			}

			foreach (var type in nonStaticTypes)
			{
				var dm = DeserializerCodegen.GenerateStaticDeserializerStub(tb, type);
				map[type].ReaderMethodInfo = dm;
				map[type].ReaderILGen = dm.GetILGenerator();
			}

			var ctx = new CodeGenContext(map);

			/* generate bodies */
			foreach (var type in nonStaticTypes)
			{
				SerializerCodegen.GenerateSerializerBody(ctx, type, map[type].WriterILGen);
				DeserializerCodegen.GenerateDeserializerBody(ctx, type, map[type].ReaderILGen);
			}


			foreach (var kv in map)
			{
				GetSerializationInvoker(tb, kv.Value.WriterMethodInfo, kv.Key, (int)kv.Value.TypeID);
				GetDeserializationInvoker(tb, kv.Value.ReaderMethodInfo, kv.Key, (int)kv.Value.TypeID);
			}
			tb.CreateType();
			ab.Save("NetSerializer2Debug.dll");
			typeID = SerializationID.typeIDstart;
		}
#endif

		public static void _SerializerSwitch(Serializer serializer, Stream stream, object value, ObjectList objList)
		{
			if (objList != null)
			{
				int index = objList.IndexOf(value);
				if (index != -1)
				{
					value = new ObjectRef(index);
				}
			}

			if (value == null)
			{
				Primitives.WritePrimitive(serializer, stream, (uint)0, objList);
			}
			else
			{
				TypeData typeData;

#if NEW_LCK
				var vType = value.GetType();
				bool found;
				serializer.m_lck.EnterReadLock();
				try {
					found = serializer.m_type_TypeData.TryGetValue(vType, out typeData);
				}
				finally{
					serializer.m_lck.ExitReadLock();
				}

	            if (!found)
	            {
					if (!serializer.m_autoRegister)
						throw new InvalidOperationException(String.Format("Unknown type = {0}", value.GetType().FullName));

					var ctypes = serializer.CollectTypes(new[] { vType });

					serializer.m_lck.EnterWriteLock();
					try
					{
						var types = ctypes.Select(v => v).Where(v => !serializer.m_type_TypeData.ContainsKey(v)).ToArray<Type>();
						var map_Type2TypeData = serializer.GenerateDynamic(types, null);
						foreach (var kv in map_Type2TypeData)
						{
							serializer.m_type_TypeData.Add(kv.Key, kv.Value);
							serializer.m_typeID_TypeData.Add(kv.Value.TypeID, kv.Value);
							if (kv.Value.TypeID >= SerializationID.typeIDstart)
								serializer.m_registredTypeData.Add(kv.Value);
						}
					}
					finally
					{
						serializer.m_lck.ExitWriteLock();
					}
	            }
#else
				lock (serializer)
				{
					var v_type = value.GetType();
					if (!serializer.m_type_TypeData.TryGetValue(v_type, out typeData))
					{
						if (!serializer.m_autoRegister)
							throw new InvalidOperationException(String.Format("Unknown type = {0}", value.GetType().FullName));
						else
						{
							var ctypes = serializer.CollectTypes(new Type[]{v_type});
							var types = ctypes.Select(v => v).Where(v => !serializer.m_type_TypeData.ContainsKey(v)).ToArray<Type>();

							var map_Type2TypeData = serializer.GenerateDynamic(types, null);
							foreach (var kv in map_Type2TypeData)
							{
								serializer.m_type_TypeData.Add(kv.Key, kv.Value);
								serializer.m_typeID_TypeData.Add(kv.Value.TypeID, kv.Value);
								if (kv.Value.TypeID >= SerializationID.typeIDstart)
									serializer.m_registredTypeData.Add(kv.Value);
							}
						}
					}
				}
#endif

				Primitives.WritePrimitive(serializer, stream, typeData.TypeID, objList);
				typeData.serializer(serializer, stream, value, objList);
			}

		}


		public static void _DeserializerSwitch(Serializer serializer, Stream stream, out object value, ObjectList objList)
		{
			uint num;
			Primitives.ReadPrimitive(serializer, stream, out num, objList);

			if (num == 0)
			{
				value = null;
			}
			else if (num == 1)
			{
				ObjectRef ref2;
				Primitives.ReadPrimitive(serializer, stream, out ref2, objList);
				if (objList == null)
				{
					value = null;
					return;
				}
				value = objList.GetAt(ref2);
			}
			else
			{
				TypeData typeData;
#if NEW_LCK
				serializer.m_lck.EnterReadLock();
				try
#else
				lock (serializer)
#endif
				{
					if (!serializer.m_typeID_TypeData.TryGetValue(num, out typeData))
						throw new InvalidOperationException(String.Format("Unknown typeId = {0}", num));
				}
#if NEW_LCK
				finally
				{
					serializer.m_lck.ExitReadLock();
				}
#endif
				typeData.deserializer(serializer, stream, out value, objList);
			}

		}
	
	}
}
