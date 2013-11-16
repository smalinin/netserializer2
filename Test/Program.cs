using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetSerializer2;
using System.IO;
using System.Diagnostics;

namespace Test
{
	interface INetTest
	{
		string Framework { get; }
		void Prepare(Serializer serializer, int numMessages);
		MessageBase[] Test(MessageBase[] msgs);
	}

	interface IMemStreamTest
	{
		string Framework { get; }
		void Prepare(Serializer serializer, int numMessages);
		long Serialize(MessageBase[] msgs);
		MessageBase[] Deserialize();
	}

	static class Program
	{
		static bool s_runProtoBufTests = true;
		static bool s_quickRun = false;

		static void Main(string[] args)
		{
			System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

			var types = typeof(MessageBase).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MessageBase)))
				.Concat(new Type[] { typeof(SimpleClass), typeof(SimpleClass2) })
				.ToArray();

			var serializer = new Serializer(); 
			serializer.Initialize(types);
			NetSerializer.Serializer.Initialize(types);

			Warmup(serializer);

			RunTests(serializer, typeof(U8Message), 2000000);
			RunTests(serializer, typeof(S16Message), 2000000);
			RunTests(serializer, typeof(S32Message), 2000000);
			RunTests(serializer, typeof(S64Message), 2000000);

			RunTests(serializer, typeof(PrimitivesMessage), 1000000);
			RunTests(serializer, typeof(DictionaryMessage), 5000);

			RunTests(serializer, typeof(ComplexMessage), 1000000);

			RunTests(serializer, typeof(StringMessage), 200000);

			RunTests(serializer, typeof(StructMessage), 1000000);

			RunTests(serializer, typeof(BoxedPrimitivesMessage), 1000000);

			RunTests(serializer, typeof(ByteArrayMessage), 3000);
			RunTests(serializer, typeof(IntArrayMessage), 400);

			//Console.WriteLine("Press enter to quit");
			//Console.ReadLine();
		}

		static void Warmup(Serializer serializer)
		{
			var msgs = new MessageBase[] { new S16Message(), new ComplexMessage(), new IntArrayMessage() };

			IMemStreamTest t;

			t = new MemStreamTest();
			t.Prepare(serializer, msgs.Length);
			t.Serialize(msgs);
			t.Deserialize();

			t = new MemStreamTest2();
			t.Prepare(serializer, msgs.Length);
			t.Serialize(msgs);
			t.Deserialize();

			if (s_runProtoBufTests)
			{
				t = new PBMemStreamTest();
				t.Prepare(serializer, msgs.Length);
				t.Serialize(msgs);
				t.Deserialize();
			}
		}

		static void RunTests(Serializer serializer, Type msgType, int numMessages)
		{
			if (s_quickRun)
				numMessages = 50;

			Console.WriteLine("== {0} {1} ==", numMessages, msgType.Name);

			bool protobufCompatible = msgType.GetCustomAttributes(typeof(ProtoBuf.ProtoContractAttribute), false).Any();

			var msgs = MessageBase.CreateMessages(msgType, numMessages);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Test(serializer, new MemStreamTest(), msgs);
			Test(serializer, new MemStreamTest2(), msgs);
			if (s_runProtoBufTests && protobufCompatible)
				Test(serializer, new PBMemStreamTest(), msgs);

			Test(serializer, new NetTest(), msgs);
			Test(serializer, new NetTest2(), msgs);
			if (s_runProtoBufTests && protobufCompatible)
				Test(serializer, new PBNetTest(), msgs);
		}

		static void Test(Serializer serializer, IMemStreamTest test, MessageBase[] msgs)
		{
			test.Prepare(serializer, msgs.Length);

			/* Serialize part */
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var c0 = GC.CollectionCount(0);
				var c1 = GC.CollectionCount(1);
				var c2 = GC.CollectionCount(2);

				var sw = Stopwatch.StartNew();

				long size = test.Serialize(msgs);

				sw.Stop();

				c0 = GC.CollectionCount(0) - c0;
				c1 = GC.CollectionCount(1) - c1;
				c2 = GC.CollectionCount(2) - c2;

				Console.WriteLine("{0,-14}| {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
					test.Framework, "MemStream Serialize", sw.ElapsedMilliseconds, c0, c1, c2, size);
			}

			/* Deerialize part */

			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var c0 = GC.CollectionCount(0);
				var c1 = GC.CollectionCount(1);
				var c2 = GC.CollectionCount(2);

				var sw = Stopwatch.StartNew();

				var received = test.Deserialize();

				sw.Stop();

				c0 = GC.CollectionCount(0) - c0;
				c1 = GC.CollectionCount(1) - c1;
				c2 = GC.CollectionCount(2) - c2;

				Console.WriteLine("{0,-14}| {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
					test.Framework, "MemStream Deserialize", sw.ElapsedMilliseconds, c0, c1, c2, "");

				for (int i = 0; i < msgs.Length; ++i)
				{
					var msg1 = msgs[i];
					var msg2 = received[i];

					msg1.Compare(msg2);
				}
			}
		}

		static void Test(Serializer serializer, INetTest test, MessageBase[] msgs)
		{
			test.Prepare(serializer, msgs.Length);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var c0 = GC.CollectionCount(0);
			var c1 = GC.CollectionCount(1);
			var c2 = GC.CollectionCount(2);

			var sw = Stopwatch.StartNew();

			var received = test.Test(msgs);

			sw.Stop();

			c0 = GC.CollectionCount(0) - c0;
			c1 = GC.CollectionCount(1) - c1;
			c2 = GC.CollectionCount(2) - c2;

			Console.WriteLine("{0,-14}| {1,-21} | {2,10} | {3,3} {4,3} {5,3} | {6,10} |",
				test.Framework, "NetTest", sw.ElapsedMilliseconds, c0, c1, c2, "");

			for (int i = 0; i < msgs.Length; ++i)
			{
				var msg1 = msgs[i];
				var msg2 = received[i];

				msg1.Compare(msg2);
			}
		}
	}
}
