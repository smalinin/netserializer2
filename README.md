NetSerializer2
==============

Fast .Net Serializer  (based on https://github.com/tomba/netserializer)

#####New features in NetSerializer2:


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


#####Performance

Below is a performance comparison between NetSerializer2, NetSerializer and protobuf-net.  
NetSerializer the original project - https://github.com/tomba/netserializer .  
Protobuf-net is a fast Protocol Buffers compatible serializer.  


The table lists the time it takes run the test, the number of GC collections
(per generation) that happened during the test, and the size of the
outputted serialized data (when available).


######There are three tests:

- MemStream Serialize - serializes an array of objects to a memory stream.

- MemStream Deserialize - deserializes the stream created with MemStream
  Serialize test.

- NetTest - uses two threads, of which the first one serializes objects and
  sends them over a local socket, and the second one receive the data and
  deserialize the objects. Note that the size is not available for NetTest, as
  tracking the sent data is not trivial. However, the dataset is the same as
  with MemStream, an so is the size of the data.

The tests are ran for different kinds of datasets. These datasets are composed
of objects of the same type. However, each object is initialized with random
data. The types used in the datasets are:

- U8Message - contains a single byte field, initialized always to 0
- S16Message - contains a single short field
- S32Message - contains a single int field
- U32Message - contains a single uint field
- S64Message - contains a single long field
- PrimitivesMessage - contains multiple fields of primitive types
- DictionaryMessage - contains multiple fields of dictionary types
- ComplexMessage - contains fields with interface and abstract references
- StringMessage - contains random length string
- StructMessage -
- BoxedPrimitivesMessage -
- ByteArrayMessage - contains random length byte array
- IntArrayMessage - contains random length int array

The details of the tests can be found from the source code. The tests were run
on a 64bit Windows 8 box.

                                          |  time (ms) |    GC coll. |   size (B) |
    == 2000000 U8Message ==
    NetSerializer | MemStream Serialize   |         93 |   0   0   0 |    4000000 |
    NetSerializer | MemStream Deserialize |        280 |   8   4   1 |            |
    NetSerializer2| MemStream Serialize   |        115 |   0   0   0 |    4000000 |
    NetSerializer2| MemStream Deserialize |        322 |   8   4   1 |            |
    protobuf-net  | MemStream Serialize   |        552 |  92   1   1 |   10984586 |
    protobuf-net  | MemStream Deserialize |       1171 |  72  19   1 |            |
    NetSerializer | NetTest               |        341 |   9   4   1 |            |
    NetSerializer2| NetTest               |        418 |   8   4   0 |            |
    protobuf-net  | NetTest               |      15618 | 135  34   1 |            |
     
    == 2000000 S16Message ==
    NetSerializer | MemStream Serialize   |        121 |   0   0   0 |    7495612 |
    NetSerializer | MemStream Deserialize |        309 |   8   4   1 |            |
    NetSerializer2| MemStream Serialize   |        147 |   0   0   0 |    7495612 |
    NetSerializer2| MemStream Deserialize |        346 |   8   4   1 |            |
    protobuf-net  | MemStream Serialize   |        616 |  92   2   1 |   20503854 |
    protobuf-net  | MemStream Deserialize |       1257 |  72  19   1 |            |
    NetSerializer | NetTest               |        493 |   9   4   1 |            |
    NetSerializer2| NetTest               |        485 |   9   4   1 |            |
    protobuf-net  | NetTest               |      15386 | 135  34   1 |            |
     
    == 2000000 S32Message ==
    NetSerializer | MemStream Serialize   |        156 |   1   1   1 |   11874591 |
    NetSerializer | MemStream Deserialize |        325 |   8   4   1 |            |
    NetSerializer2| MemStream Serialize   |        193 |   1   1   1 |   11874591 |
    NetSerializer2| MemStream Deserialize |        357 |   8   4   1 |            |
    protobuf-net  | MemStream Serialize   |        546 |  92   1   1 |   17748852 |
    protobuf-net  | MemStream Deserialize |       1245 |  72  19   1 |            |
    NetSerializer | NetTest               |        538 |   9   5   1 |            |
    NetSerializer2| NetTest               |        489 |   8   3   0 |            |
    protobuf-net  | NetTest               |      15290 | 135  34   1 |            |
     
    == 2000000 S64Message ==
    NetSerializer | MemStream Serialize   |        235 |   1   1   1 |   20992943 |
    NetSerializer | MemStream Deserialize |        369 |   8   4   1 |            |
    NetSerializer2| MemStream Serialize   |        265 |   1   1   1 |   20992943 |
    NetSerializer2| MemStream Deserialize |        401 |   8   4   1 |            |
    protobuf-net  | MemStream Serialize   |        579 |  92   1   1 |   26992398 |
    protobuf-net  | MemStream Deserialize |       1250 |  72  19   1 |            |
    NetSerializer | NetTest               |        672 |   9   4   1 |            |
    NetSerializer2| NetTest               |        634 |   8   4   0 |            |
    protobuf-net  | NetTest               |      15527 | 135  33   1 |            |
     
    == 1000000 PrimitivesMessage ==
    NetSerializer | MemStream Serialize   |        410 |   1   1   1 |   46866121 |
    NetSerializer | MemStream Deserialize |        389 |  12   6   1 |            |
    NetSerializer2| MemStream Serialize   |        425 |   0   0   0 |   46866121 |
    NetSerializer2| MemStream Deserialize |        409 |  12   6   1 |            |
    protobuf-net  | MemStream Serialize   |        584 |  46   1   1 |   66889477 |
    protobuf-net  | MemStream Deserialize |       1054 |  44  12   1 |            |
    NetSerializer | NetTest               |        791 |  12   5   0 |            |
    NetSerializer2| NetTest               |        824 |  13   7   1 |            |
    protobuf-net  | NetTest               |       8225 |  75  19   1 |            |
     
    == 5000 DictionaryMessage ==
    NetSerializer | MemStream Serialize   |        816 |  16   2   2 |   51974300 |
    NetSerializer | MemStream Deserialize |       1205 |  56  20   2 |            |
    NetSerializer2| MemStream Serialize   |        837 | 101   0   0 |   49445788 |
    NetSerializer2| MemStream Deserialize |       1555 | 116  29   1 |            |
    protobuf-net  | MemStream Serialize   |       1028 |  85   2   1 |   76703724 |
    protobuf-net  | MemStream Deserialize |       2271 | 171  39   1 |            |
    NetSerializer | NetTest               |       1650 |  67  23   1 |            |
    NetSerializer2| NetTest               |       1937 | 199  36   0 |            |
    protobuf-net  | NetTest               |       2493 | 273  42   1 |            |
     
    == 1000000 ComplexMessage ==
    NetSerializer | MemStream Serialize   |        451 |   1   1   1 |   32157368 |
    NetSerializer | MemStream Deserialize |        931 |  26  13   1 |            |
    NetSerializer2| MemStream Serialize   |        497 |   0   0   0 |   32157368 |
    NetSerializer2| MemStream Deserialize |       1066 |  26  12   1 |            |
    protobuf-net  | MemStream Serialize   |        810 |  45   0   0 |   55093482 |
    protobuf-net  | MemStream Deserialize |       1902 |  58  15   1 |            |
    NetSerializer | NetTest               |       1200 |  26  13   1 |            |
    NetSerializer2| NetTest               |       1396 |  26  12   1 |            |
    protobuf-net  | NetTest               |       8785 |  89  23   1 |            |
     
    == 200000 StringMessage ==
    NetSerializer | MemStream Serialize   |         51 |   1   1   1 |   10287762 |
    NetSerializer | MemStream Deserialize |         84 |   5   2   0 |            |
    NetSerializer2| MemStream Serialize   |         78 |   9   1   1 |   10091742 |
    NetSerializer2| MemStream Deserialize |        150 |  11   4   1 |            |
    protobuf-net  | MemStream Serialize   |        108 |  11   2   2 |   10687742 |
    protobuf-net  | MemStream Deserialize |        218 |  19   5   1 |            |
    NetSerializer | NetTest               |        101 |   5   2   0 |            |
    NetSerializer2| NetTest               |        161 |  19   5   0 |            |
    protobuf-net  | NetTest               |       1680 |  25   6   1 |            |
     
    == 1000000 StructMessage ==
    NetSerializer | MemStream Serialize   |        298 |   1   1   1 |   28317423 |
    NetSerializer | MemStream Deserialize |        479 |  16   7   1 |            |
    NetSerializer2| MemStream Serialize   |        342 |  35   0   0 |   27367608 |
    NetSerializer2| MemStream Deserialize |        690 |  39  11   1 |            |
    protobuf-net  | MemStream Serialize   |        619 |  62   1   1 |   38728030 |
    protobuf-net  | MemStream Deserialize |       1288 | 101  26   1 |            |
    NetSerializer | NetTest               |        655 |  16   8   1 |            |
    NetSerializer2| NetTest               |        805 |  65  17   0 |            |
    protobuf-net  | NetTest               |       8701 | 142  37   1 |            |
     
    == 1000000 BoxedPrimitivesMessage ==
    NetSerializer | MemStream Serialize   |        390 |   0   0   0 |   18873862 |
    NetSerializer | MemStream Deserialize |        703 |  29  15   1 |            |
    NetSerializer2| MemStream Serialize   |        471 |   0   0   0 |   18873862 |
    NetSerializer2| MemStream Deserialize |        807 |  29  15   1 |            |
    NetSerializer | NetTest               |        918 |  29  14   1 |            |
    NetSerializer2| NetTest               |       1119 |  28  14   0 |            |
     
    == 3000 ByteArrayMessage ==
    NetSerializer | MemStream Serialize   |        118 |   1   1   1 |  151507768 |
    NetSerializer | MemStream Deserialize |        118 |  18   9   1 |            |
    NetSerializer2| MemStream Serialize   |        123 |   1   1   1 |  151507768 |
    NetSerializer2| MemStream Deserialize |        115 |  18   9   1 |            |
    protobuf-net  | MemStream Serialize   |        289 |  41   3   3 |  151527804 |
    protobuf-net  | MemStream Deserialize |        125 |  18   9   1 |            |
    NetSerializer | NetTest               |        234 |  17   8   0 |            |
    NetSerializer2| NetTest               |        249 |  18   9   1 |            |
    protobuf-net  | NetTest               |        430 |  50  14   3 |            |
     
    == 400 IntArrayMessage ==
    NetSerializer | MemStream Serialize   |        747 |   1   1   1 |   95992613 |
    NetSerializer | MemStream Deserialize |        407 |   2   1   1 |            |
    NetSerializer2| MemStream Serialize   |        745 |   1   1   1 |   95992613 |
    NetSerializer2| MemStream Deserialize |        430 |   2   1   1 |            |
    protobuf-net  | MemStream Serialize   |        629 |  14   3   3 |  114211488 |
    protobuf-net  | MemStream Deserialize |        492 |  10   3   1 |            |
    NetSerializer | NetTest               |       1215 |   1   1   1 |            |
    NetSerializer2| NetTest               |       1247 |   2   1   1 |            |
    protobuf-net  | NetTest               |        805 |  23   4   3 |            |
     
