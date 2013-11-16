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
using System.Linq;
using System.Text;

namespace NetSerializer2
{
	[Serializable]
	public struct ObjectRef
	{
		public int obj_ref;

		public ObjectRef(int obj_ref) 
		{
			this.obj_ref = obj_ref;
		}
	}
	
	
	public class ObjectList
	{
		private int size;
		private object[] elementData = new object[8];

		public void Add(object o)
		{
			if (elementData.Length == size)
			{
				//grow array if necessary
				Array.Resize(ref elementData, elementData.Length * 2);
			}
			elementData[size] = o;
			size++;
		}

		public int Count
		{
			get
			{
				return size;
			}
		}

		public int IndexOf(Object obj)
		{
			if (obj == null)
				return -1;

			Type type = obj.GetType();
			if (!type.IsClass)
					return -1;

			for (int i = 0; i < size; i++)
			{
				if (Object.ReferenceEquals(obj, (Object)elementData[i]))
					return i;
			}
			return -1;
		}

		public object GetAt(int index)
		{
			if (index >= size)
				throw new ArgumentOutOfRangeException("index");
			return elementData[index];
		}

		public object GetAt(ObjectRef oref)
		{
			if (oref.obj_ref >= size)
				throw new ArgumentException("Couldn't found obj_ref");
			return elementData[oref.obj_ref];
		}

	}
}
