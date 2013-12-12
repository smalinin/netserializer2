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
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NetSerializer2
{
	public static class Primitives
	{
		static uint EncodeZigZag32(int n)
		{
			return (uint)((n << 1) ^ (n >> 31));
		}

		static ulong EncodeZigZag64(long n)
		{
			return (ulong)((n << 1) ^ (n >> 63));
		}

		static int DecodeZigZag32(uint n)
		{
			return (int)(n >> 1) ^ -(int)(n & 1);
		}

		static long DecodeZigZag64(ulong n)
		{
			return (long)(n >> 1) ^ -(long)(n & 1);
		}

		static internal char ReadVarChar(Stream stream)
		{
			int result = 0;
			int offset = 0;

			for (; offset < 16; offset += 7)
			{
				int b = stream.ReadByte();
				if (b == -1)
					throw new EndOfStreamException();

				result |= (b & 0x7f) << offset;

				if ((b & 0x80) == 0)
					return (char)result;
			}
			throw new InvalidDataException();
		}

		static uint ReadVarint32(Stream stream)
		{
			int result = 0;
			int offset = 0;

			for (; offset < 32; offset += 7)
			{
				int b = stream.ReadByte();
				if (b == -1)
					throw new EndOfStreamException();

				result |= (b & 0x7f) << offset;

				if ((b & 0x80) == 0)
					return (uint)result;
			}

			throw new InvalidDataException();
	 	}

		static internal void WriteVarChar(Stream stream, char value)
		{
			for (; value >= 0x80u; value >>= 7)
				stream.WriteByte((byte)(value | 0x80u));

			stream.WriteByte((byte)value);
		}

		static void WriteVarint32(Stream stream, uint value)
		{
			for (; value >= 0x80u; value >>= 7)
				stream.WriteByte((byte)(value | 0x80u));

			stream.WriteByte((byte)value);
		}


		static ulong ReadVarint64(Stream stream)
		{
			long result = 0;
			int offset = 0;

			for (; offset < 64; offset += 7)
			{
				int b = stream.ReadByte();
				if (b == -1)
					throw new EndOfStreamException();

				result |= ((long)(b & 0x7f)) << offset;

				if ((b & 0x80) == 0)
					return (ulong)result;
			}

			throw new InvalidDataException();
		}

		static void WriteVarint64(Stream stream, ulong value)
		{
			for (; value >= 0x80u; value >>= 7)
				stream.WriteByte((byte)(value | 0x80u));

			stream.WriteByte((byte)value);
		}


		public static void WritePrimitive(Serializer serializer, Stream stream, bool value, ObjectList objList)
		{
			stream.WriteByte(value ? (byte)1 : (byte)0);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out bool value, ObjectList objList)
		{
			var b = stream.ReadByte();
			value = b != 0;
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, byte value, ObjectList objList)
		{
			stream.WriteByte(value);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out byte value, ObjectList objList)
		{
			value = (byte)stream.ReadByte();
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, sbyte value, ObjectList objList)
		{
			stream.WriteByte((byte)value);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out sbyte value, ObjectList objList)
		{
			value = (sbyte)stream.ReadByte();
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, char value, ObjectList objList)
		{
			WriteVarChar(stream, value);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out char value, ObjectList objList)
		{
			value = ReadVarChar(stream);
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, ushort value, ObjectList objList)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out ushort value, ObjectList objList)
		{
			value = (ushort)ReadVarint32(stream);
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, short value, ObjectList objList)
		{
			WriteVarint32(stream, EncodeZigZag32(value));
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out short value, ObjectList objList)
		{
			value = (short)DecodeZigZag32(ReadVarint32(stream));
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, uint value, ObjectList objList)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out uint value, ObjectList objList)
		{
			value = ReadVarint32(stream);
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, int value, ObjectList objList)
		{
			WriteVarint32(stream, EncodeZigZag32(value));
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out int value, ObjectList objList)
		{
			value = DecodeZigZag32(ReadVarint32(stream));
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, ulong value, ObjectList objList)
		{
			WriteVarint64(stream, value);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out ulong value, ObjectList objList)
		{
			value = ReadVarint64(stream);
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, long value, ObjectList objList)
		{
			WriteVarint64(stream, EncodeZigZag64(value));
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out long value, ObjectList objList)
		{
			value = DecodeZigZag64(ReadVarint64(stream));
		}

#if !NO_UNSAFE
		public static unsafe void WritePrimitive(Serializer serializer, Stream stream, float value, ObjectList objList)
		{
			uint v = *(uint*)(&value);
			WriteVarint32(stream, v);
		}

		public static unsafe void ReadPrimitive(Serializer serializer, Stream stream, out float value, ObjectList objList)
		{
			uint v = ReadVarint32(stream);
			value = *(float*)(&v);
		}

		public static unsafe void WritePrimitive(Serializer serializer, Stream stream, double value, ObjectList objList)
		{
			ulong v = *(ulong*)(&value);
			WriteVarint64(stream, v);
		}

		public static unsafe void ReadPrimitive(Serializer serializer, Stream stream, out double value, ObjectList objList)
		{
			ulong v = ReadVarint64(stream);
			value = *(double*)(&v);
		}
#else
		public static void WritePrimitive(Serializer serializer, Stream stream, float value, ObjectList objList)
		{
			WritePrimitive(stream, (double)value);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out float value, ObjectList objList)
		{
			double v;
			ReadPrimitive(stream, out v);
			value = (float)v;
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, double value, ObjectList objList)
		{
			ulong v = (ulong)BitConverter.DoubleToInt64Bits(value);
			WriteVarint64(stream, v);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out double value, ObjectList objList)
		{
			ulong v = ReadVarint64(stream);
			value = BitConverter.Int64BitsToDouble(v);
		}
#endif

		public static void WritePrimitive(Serializer serializer, Stream stream, DateTime value, ObjectList objList)
		{
			long v = value.ToBinary();
			WritePrimitive(serializer, stream, v, objList);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out DateTime value, ObjectList objList)
		{
			long v;
			ReadPrimitive(serializer, stream, out v, objList);
			value = DateTime.FromBinary(v);
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, string value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}

			if (objList != null)
				objList.Add(value);

			var encoding = new UTF8Encoding(false, true);
			int len = encoding.GetByteCount(value);

			WritePrimitive(serializer, stream, (uint)len + 1, objList);

			var buf = new byte[len];

			encoding.GetBytes(value, 0, value.Length, buf, 0);
			stream.Write(buf, 0, len);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out string value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}
			else if (len == 1)
			{
				value = string.Empty;
				return;
			}

			len--;

			var encoding = new UTF8Encoding(false, true);
			var buf = new byte[len];
			int l = 0;

			while (l < len)
			{
				int r = stream.Read(buf, l, (int)len - l);
				if (r == 0)
					throw new EndOfStreamException();
				l += r;
			}
			value = encoding.GetString(buf);

			if (objList != null)
				objList.Add(value);
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, byte[] value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			WritePrimitive(serializer, stream, (uint)value.Length + 1, objList);
			stream.Write(value, 0, value.Length);
		}

		static readonly byte[] s_emptyByteArray = new byte[0];

		public static void ReadPrimitive(Serializer serializer, Stream stream, out byte[] value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}
			else if (len == 1) 
			{
				value = s_emptyByteArray;
				return;
			}

			len--;

			value = new byte[len];
			int l = 0;

			while (l < len)
			{
				int r = stream.Read(value, l, (int)len - l);
				if (r == 0)
					throw new EndOfStreamException();
				l += r;
			}
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive(Serializer serializer, Stream stream, int[] value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			WritePrimitive(serializer, stream, (uint)value.Length + 1, objList);
			for (uint i = 0; i < value.Length; ++i)
				WritePrimitive(serializer, stream, value[i], objList);
		}

		static readonly int[] s_emptyIntArray = new int[0];

		public static void ReadPrimitive(Serializer serializer, Stream stream, out int[] value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}
			else if (len == 1)
			{
				value = s_emptyIntArray;
				return;
			}

			len--;

			value = new int[len];
			for (uint i = 0; i < len; ++i)
				ReadPrimitive(serializer, stream, out value[i], objList);
			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive(Serializer serializer, Stream stream, TimeSpan value, ObjectList objList)
		{
			long v = value.Ticks;
			WritePrimitive(serializer, stream, v, objList);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out TimeSpan value, ObjectList objList)
		{
			long v;
			ReadPrimitive(serializer, stream, out v, objList);
			value = TimeSpan.FromTicks(v);
		}


		public static void WritePrimitive(Serializer serializer, Stream stream, DateTimeOffset value, ObjectList objList)
		{
			long v = value.DateTime.ToBinary();
			long o = value.Offset.Ticks;
			WritePrimitive(serializer, stream, v, objList);
			WritePrimitive(serializer, stream, o, objList);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out DateTimeOffset value, ObjectList objList)
		{
			long v,o;
			ReadPrimitive(serializer, stream, out v, objList);
			ReadPrimitive(serializer, stream, out o, objList);
			value = new DateTimeOffset(DateTime.FromBinary(v), TimeSpan.FromTicks(o));
		}


		public static void WritePrimitive(Serializer serializer, Stream stream, decimal value, ObjectList objList)
		{
			int[] v = Decimal.GetBits(value);
			WritePrimitive(serializer, stream, v[0], null);
			WritePrimitive(serializer, stream, v[1], null);
			WritePrimitive(serializer, stream, v[2], null);
			WritePrimitive(serializer, stream, v[3], null);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out decimal value, ObjectList objList)
		{
			var v = new int[4];
			ReadPrimitive(serializer, stream, out v[0], null);
			ReadPrimitive(serializer, stream, out v[1], null);
			ReadPrimitive(serializer, stream, out v[2], null);
			ReadPrimitive(serializer, stream, out v[3], null);
			value = new Decimal(v);
		}

		public static void WritePrimitive(Serializer serializer, Stream stream, Guid value, ObjectList objList)
		{
			byte[] v = value.ToByteArray();
			WritePrimitive(serializer, stream, v, null);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out Guid value, ObjectList objList)
		{
			byte[] v;
			ReadPrimitive(serializer, stream, out v, objList);
			value = new Guid(v);
		}


		public static void WritePrimitive(Serializer serializer, Stream stream, ObjectRef value, ObjectList objList)
		{
			WritePrimitive(serializer, stream, value.obj_ref, objList);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out ObjectRef value, ObjectList objList)
		{
			ReadPrimitive(serializer, stream, out value.obj_ref, objList);
		}


		public static void WritePrimitive<TKey, TValue>(Serializer serializer, Stream stream, Dictionary<TKey, TValue> value, ObjectList objList)
		{
#if OLD_DICT
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
#else
			if (objList != null)
				objList.Add(value);

			var kvpArray = new KeyValuePair<TKey, TValue>[value.Count];

			int i = 0;
			foreach (var kvp in value)
				kvpArray[i++] = kvp;

			serializer.Serialize(stream, kvpArray, objList);
#endif
		}

		public static void ReadPrimitive<TKey, TValue>(Serializer serializer, Stream stream, out Dictionary<TKey, TValue> value, ObjectList objList)
		{
#if OLD_DICT
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Dictionary<TKey, TValue>((int)len);
			for (int i = 0; i < len; i++)
			{
				var kvp = (KeyValuePair<TKey, TValue>)serializer.Deserialize(stream, objList);
				value.Add(kvp.Key, kvp.Value);
			}
#else
			var kvpArray = (KeyValuePair<TKey, TValue>[])serializer.Deserialize(stream, objList);

			value = new Dictionary<TKey, TValue>(kvpArray.Length);

			foreach (var kvp in kvpArray)
				value.Add(kvp.Key, kvp.Value);
#endif

			if (objList != null)
				objList.Add(value);
		}


		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, TValue? value, ObjectList objList) where TValue : struct
		{
			if (!value.HasValue)
			{
				WritePrimitive(serializer, stream, (byte)0, objList);
				return;
			}

			WritePrimitive(serializer, stream, (byte)1, objList);
			serializer.Serialize(stream, value.Value, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out TValue? value, ObjectList objList) where TValue : struct
		{
			byte ntype;
			ReadPrimitive(serializer, stream, out ntype, objList);

			if (ntype == 0)
			{
				value = new Nullable<TValue>();
				return;
			}

			TValue v = (TValue)serializer.Deserialize(stream, objList);
			value = new Nullable<TValue>(v);
		}

//??????????????????????????????????????
		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, List<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out List<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new List<TValue>((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Add((TValue) serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


/////////////////////////////////
		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, HashSet<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out HashSet<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new HashSet<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Add((TValue)serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////
		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, Queue<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out Queue<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Queue<TValue>((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Enqueue((TValue)serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////
		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, Stack<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out Stack<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Stack<TValue>((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Push((TValue)serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////
		public static void WritePrimitive<TKey, TValue>(Serializer serializer, Stream stream, SortedDictionary<TKey, TValue> value, ObjectList objList)
		{
#if OLD_DICT

			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
#else
			if (objList != null)
				objList.Add(value);

			var kvpArray = new KeyValuePair<TKey, TValue>[value.Count];

			int i = 0;
			foreach (var kvp in value)
				kvpArray[i++] = kvp;

			serializer.Serialize(stream, kvpArray, objList);
#endif
		}

		public static void ReadPrimitive<TKey, TValue>(Serializer serializer, Stream stream, out SortedDictionary<TKey, TValue> value, ObjectList objList)
		{
#if OLD_DICT
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new SortedDictionary<TKey, TValue>();
			for (int i = 0; i < len; i++)
			{
				var kvp = (KeyValuePair<TKey, TValue>)serializer.Deserialize(stream, objList);
				value.Add(kvp.Key, kvp.Value);
			}
#else
			var kvpArray = (KeyValuePair<TKey, TValue>[])serializer.Deserialize(stream, objList);

			value = new SortedDictionary<TKey, TValue>();

			foreach (var kvp in kvpArray)
				value.Add(kvp.Key, kvp.Value);
#endif
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////
		public static void WritePrimitive<TKey, TValue>(Serializer serializer, Stream stream, SortedList<TKey, TValue> value, ObjectList objList)
		{
#if OLD_DICT
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
#else
			if (objList != null)
				objList.Add(value);

			var kvpArray = new KeyValuePair<TKey, TValue>[value.Count];

			int i = 0;
			foreach (var kvp in value)
				kvpArray[i++] = kvp;

			serializer.Serialize(stream, kvpArray, objList);
#endif
		}

		public static void ReadPrimitive<TKey, TValue>(Serializer serializer, Stream stream, out SortedList<TKey, TValue> value, ObjectList objList)
		{
#if OLD_DICT
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new SortedList<TKey, TValue>((int)len);
			for (int i = 0; i < len; i++)
			{
				var kvp = (KeyValuePair<TKey, TValue>)serializer.Deserialize(stream, objList);
				value.Add(kvp.Key, kvp.Value);
			}
#else
			var kvpArray = (KeyValuePair<TKey, TValue>[])serializer.Deserialize(stream, objList);

			value = new SortedList<TKey, TValue>(kvpArray.Length);

			foreach (var kvp in kvpArray)
				value.Add(kvp.Key, kvp.Value);
#endif
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////
		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, SortedSet<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out SortedSet<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new SortedSet<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Add((TValue)serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

//////////////////////////////////////
		public static void WritePrimitive<TKey, TValue>(Serializer serializer, Stream stream, ConcurrentDictionary<TKey, TValue> value, ObjectList objList)
		{
#if OLD_DICT
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
#else
			if (objList != null)
				objList.Add(value);

			var kvpArray = new KeyValuePair<TKey, TValue>[value.Count];

			int i = 0;
			foreach (var kvp in value)
				kvpArray[i++] = kvp;

			serializer.Serialize(stream, kvpArray, objList);
#endif
		}

		public static void ReadPrimitive<TKey, TValue>(Serializer serializer, Stream stream, out ConcurrentDictionary<TKey, TValue> value, ObjectList objList)
		{
#if OLD_DICT
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new ConcurrentDictionary<TKey, TValue>(8, (int)len);
			for (int i = 0; i < len; i++)
			{
				var kvp = (KeyValuePair<TKey, TValue>)serializer.Deserialize(stream, objList);
				value.TryAdd(kvp.Key, kvp.Value);
			}
#else
			var kvpArray = (KeyValuePair<TKey, TValue>[])serializer.Deserialize(stream, objList);

			value = new ConcurrentDictionary<TKey, TValue>(8, kvpArray.Length);

			foreach (var kvp in kvpArray)
				value.TryAdd(kvp.Key, kvp.Value);
#endif
			if (objList != null)
				objList.Add(value);
		}

/////////////////////////////////////////////////////////
		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, ConcurrentBag<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out ConcurrentBag<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new ConcurrentBag<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Add((TValue)serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


//////////!
		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, ConcurrentQueue<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out ConcurrentQueue<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new ConcurrentQueue<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Enqueue((TValue)serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


/////////////!
		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, ConcurrentStack<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out ConcurrentStack<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new ConcurrentStack<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Push((TValue)serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

		//BlockingCollection
		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, BlockingCollection<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out BlockingCollection<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new BlockingCollection<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.Add((TValue)serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


		//LinkedList
		public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, LinkedList<TValue> value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out LinkedList<TValue> value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new LinkedList<TValue>();
			for (int i = 0; i < len; i++)
			{
				value.AddLast((TValue)serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}





/////////////////!
		public static void WritePrimitive(Serializer serializer, Stream stream, ArrayList value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out ArrayList value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new ArrayList((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Add(serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////!
		public static void WritePrimitive(Serializer serializer, Stream stream, Hashtable value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (DictionaryEntry kvp in value)
			{
				serializer.Serialize(stream, kvp.Key, objList);
				serializer.Serialize(stream, kvp.Value, objList);
			}
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out Hashtable value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Hashtable((int)len);
			for (int i = 0; i < len; i++)
			{
				object _Key = serializer.Deserialize(stream, objList);
				object _Val = serializer.Deserialize(stream, objList);
				value.Add(_Key, _Val);
			}
			if (objList != null)
				objList.Add(value);
		}


/////////////////!
		public static void WritePrimitive(Serializer serializer, Stream stream, Queue value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out Queue value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Queue((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Enqueue(serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


/////////////////!
		public static void WritePrimitive(Serializer serializer, Stream stream, Stack value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (var kvp in value)
				serializer.Serialize(stream, kvp, objList);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out Stack value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new Stack((int)len);
			for (int i = 0; i < len; i++)
			{
				value.Push(serializer.Deserialize(stream, objList));
			}
			if (objList != null)
				objList.Add(value);
		}


/////////////////!
		public static void WritePrimitive(Serializer serializer, Stream stream, SortedList value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			foreach (DictionaryEntry kvp in value)
			{
				serializer.Serialize(stream, kvp.Key, objList);
				serializer.Serialize(stream, kvp.Value, objList);
			}
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out SortedList value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			value = new SortedList((int)len);
			for (int i = 0; i < len; i++)
			{
				object _Key = serializer.Deserialize(stream, objList);
				object _Val = serializer.Deserialize(stream, objList);
				value.Add(_Key, _Val);
			}
			if (objList != null)
				objList.Add(value);
		}

/////////////////!
		public static void WritePrimitive(Serializer serializer, Stream stream, BitArray value, ObjectList objList)
		{
			if (value == null)
			{
				WritePrimitive(serializer, stream, (uint)0, objList);
				return;
			}
			if (objList != null)
				objList.Add(value);

			int count = value.Count;
			WritePrimitive(serializer, stream, (uint)count + 1, objList);

			int numints = (value.Count + 31) / 32;
			WritePrimitive(serializer, stream, (uint)numints, objList);

			int[] data = new int[numints];
			value.CopyTo(data, 0);
			foreach (var v in data)
				WritePrimitive(serializer, stream, v, objList);
		}

		public static void ReadPrimitive(Serializer serializer, Stream stream, out BitArray value, ObjectList objList)
		{
			uint len;
			ReadPrimitive(serializer, stream, out len, objList);

			if (len == 0)
			{
				value = null;
				return;
			}

			len--;

			uint numints;
			ReadPrimitive(serializer, stream, out numints, null);

			int[] data = new int[numints];
			for (uint i = 0; i < numints; i++)
				ReadPrimitive(serializer, stream, out data[i], null);

			value = new BitArray(data);
			value.Length = (int)len;

			if (objList != null)
				objList.Add(value);
		}


		/**
				public static void WritePrimitive<TValue>(Serializer serializer, Stream stream, CopyOnWriteArrayList<TValue> value, ObjectList objList)
				{
					if (value == null)
					{
						WritePrimitive(serializer, stream, (uint)0, objList);
						return;
					}
					if (objList != null)
						objList.Add(value);

					int count = value.Count;
					WritePrimitive(serializer, stream, (uint)count + 1, objList);

					foreach (var kvp in value)
						NetSerializer2.Serializer.Serialize(stream, kvp, objList);
				}

				public static void ReadPrimitive<TValue>(Serializer serializer, Stream stream, out CopyOnWriteArrayList<TValue> value, ObjectList objList)
				{
					uint len;
					ReadPrimitive(serializer, stream, out len, objList);

					if (len == 0)
					{
						value = null;
						return;
					}

					len--;

					var arr = new TValue[len];
					for (int i = 0; i < len; i++)
						arr[i] = (TValue)NetSerializer2.Serializer.Deserialize(stream, objList);
	
					value = new CopyOnWriteArrayList<TValue>(arr);
					if (objList != null)
						objList.Add(value);
				}
		***/





	}
}
