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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NetSerializer2
{
	[Serializable]
	sealed public class ClassFieldInfo
	{
		public string FieldName;
		public string TypeName;
		public bool SkipFieldAssign;
	}

	[Serializable]
	sealed public class ClassInfo
	{
		public string TypeName;
		public List<ClassFieldInfo> Fields;
		public bool UseObjectData;
		public bool IsDynamic;
		public ushort Version;
	}
	
	
	[Serializable]
	sealed class TypeData
	{
		public TypeData(uint typeID, Type type)
		{
			this.TypeID = typeID;
			this.TypeInfo = new ClassInfo { Fields = null, 
											IsDynamic = false, 
											UseObjectData = false, 
											Version = 0, 
											TypeName = Helpers.GetTypeName(type) };
		}

		public void CopyFrom(TypeData td)
		{
			this.IsDynamic = td.IsDynamic;
			this.IsInitialized = td.IsInitialized;
			this.WriterMethodInfo = td.WriterMethodInfo;
			this.WriterILGen = td.WriterILGen;
			this.ReaderMethodInfo = td.ReaderMethodInfo;
			this.ReaderILGen = td.ReaderILGen;
			this.serializer = td.serializer;
			this.deserializer = td.deserializer;
		}

		public ClassInfo TypeInfo;

		public readonly uint TypeID;
		public bool IsDynamic;
		[NonSerialized]
		public bool IsInitialized;
		[NonSerialized]
		public MethodInfo WriterMethodInfo;
		[NonSerialized]
		public ILGenerator WriterILGen;
		[NonSerialized]
		public MethodInfo ReaderMethodInfo;
		[NonSerialized]
		public ILGenerator ReaderILGen;

		[NonSerialized]
		public Serializer.SerializationInvokeHandler serializer;
		[NonSerialized]
		public Serializer.DeserializationInvokeHandler deserializer;
	}

	sealed class CodeGenContext
	{
		readonly Dictionary<Type, TypeData> m_typeMap;

		public CodeGenContext(Dictionary<Type, TypeData> typeMap)
		{
			m_typeMap = typeMap;
		}

		public MethodInfo GetWriterMethodInfo(Type type)
		{
			return m_typeMap[type].WriterMethodInfo;
		}

		public ILGenerator GetWriterILGen(Type type)
		{
			return m_typeMap[type].WriterILGen;
		}

		public MethodInfo GetReaderMethodInfo(Type type)
		{
			return m_typeMap[type].ReaderMethodInfo;
		}

		public ILGenerator GetReaderILGen(Type type)
		{
			return m_typeMap[type].ReaderILGen;
		}

		public bool IsDynamic(Type type)
		{
			return m_typeMap[type].IsDynamic;
		}
	}
}
