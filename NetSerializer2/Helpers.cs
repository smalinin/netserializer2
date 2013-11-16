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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetSerializer2
{
	static class Helpers
	{
		public static string GetTypeName(Type type)
		{
			int i;

			if (type.IsGenericType)
			{
				var args = type.GetGenericArguments();
				var genType = type.GetGenericTypeDefinition();
				var sb = new StringBuilder();
				sb.Append(genType.FullName);
				sb.Append('[');
				for (i = 0; i < args.Length; i++)
				{
					if (i > 0)
						sb.Append(',');
					sb.Append('[');
					sb.Append(GetTypeName(args[i]));
					sb.Append(']');
				}
				sb.Append("], ");

				string asName = genType.Assembly.FullName;
				i = asName.IndexOf(','); // split
				sb.Append((i >= 0) ? asName.Substring(0, i) : asName); // extract assembly only
				return sb.ToString();
			}

			string typeName = type.AssemblyQualifiedName;
			if (typeName != null)
			{
				i = typeName.IndexOf(','); // first split
				if (i >= 0) { i = typeName.IndexOf(',', i + 1); } // second split
				return (i >= 0) ? typeName.Substring(0, i) : typeName; // extract type/assembly only
			}
			return typeName;
		}


		public static bool GetPrimitives(Type containerType, Type type, out MethodInfo writer, out MethodInfo reader)
		{
			if (type.IsEnum)
				type = Enum.GetUnderlyingType(type);

			if (type.IsGenericType == false)
			{
				writer = containerType.GetMethod("WritePrimitive", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
					new Type[] { typeof(Serializer), typeof(Stream), type, typeof(ObjectList) }, null);

				reader = containerType.GetMethod("ReadPrimitive", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
					new Type[] { typeof(Serializer), typeof(Stream), type.MakeByRefType(), typeof(ObjectList) }, null);
			}
			else
			{
				var genType = type.GetGenericTypeDefinition();

				writer = GetGenWriter(containerType, genType);
				reader = GetGenReader(containerType, genType);
			}

			if (writer == null && reader == null)
				return false;
			else if (writer != null && reader != null)
				return true;
			else
				throw new InvalidOperationException(String.Format("Missing a {0}Primitive() for {1}",
					reader == null ? "Read" : "Write", type.FullName));
		}

		static MethodInfo GetGenWriter(Type containerType, Type genType)
		{
			var mis = containerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Where(mi => mi.IsGenericMethod && mi.Name == "WritePrimitive");

			foreach (var mi in mis)
			{
				var p = mi.GetParameters();

				if (p.Length != 4)
					continue;

				if (p[0].ParameterType != typeof(Serializer))
					continue;

				if (p[1].ParameterType != typeof(Stream))
					continue;

				var paramType = p[2].ParameterType;

				if (paramType.IsGenericType == false)
					continue;

				var genParamType = paramType.GetGenericTypeDefinition();

				if (genType == genParamType)
					return mi;
			}

			return null;
		}

		static MethodInfo GetGenReader(Type containerType, Type genType)
		{
			var mis = containerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Where(mi => mi.IsGenericMethod && mi.Name == "ReadPrimitive");

			foreach (var mi in mis)
			{
				var p = mi.GetParameters();

				if (p.Length != 4)
					continue;

				if (p[0].ParameterType != typeof(Serializer))
					continue;

				if (p[1].ParameterType != typeof(Stream))
					continue;

				var paramType = p[2].ParameterType;

				if (paramType.IsByRef == false)
					continue;

				paramType = paramType.GetElementType();

				if (paramType.IsGenericType == false)
					continue;

				var genParamType = paramType.GetGenericTypeDefinition();

				if (genType == genParamType)
					return mi;
			}

			return null;
		}

		public static IEnumerable<FieldInfo> GetFieldInfos(Type type)
		{
			Debug.Assert(type.IsSerializable);

			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Where(fi => (fi.Attributes & FieldAttributes.NotSerialized) == 0);

			if (type.BaseType == null)
			{
				return fields;
			}
			else
			{
				var baseFields = GetFieldInfos(type.BaseType);
				return baseFields.Concat(fields);
			}
		}
	}
}
