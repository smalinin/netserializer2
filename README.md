NetSerializer2
==============

Fast .Net Serializer  (based on https://github.com/tomba/netserializer)

New features in NetSerializer2:

- The next types were added to list of supported serialization types:
  (via WritePrimitive/ReadPrimitive)
  Nullable<T>, TimeSpan, DateTimeOffset, decimal, Guid, int[],
  ArrayList, BitArray, HashTable, Queue, Stack, SorrtedList
  List<T>, HashSet<T>, Queue<T>, Stack<T>, SortedDictionary<K,V>,
  SortedList<K,V>, SortedSet<T>, ConcurrentDictionary<K,V>,
  ConcurrentBag<T>, ConcurrentQueue<T>, ConcurrentStack<T>,
  BlockingCollection<T>, LinkedList<T>

- generated of fly version SerializationSwitch/DeserializationSwitch was replaced
  with using delegates and DynamicMethods.

- serialization/deserialization of objects with circular links was added. 
  New method SerializationDeep, DeserializationDeep

- new object may be added to Serializer later after initialization with method
  Serializer.Register(...)

- Serialization state maybe stored(restored)

- You may create two(and more) different Serializers(with different settings)
 in a one application. But note, you must decide at first, if you really need it.
 It is a very special thing.

