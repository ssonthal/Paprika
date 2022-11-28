# Paprika

A low level Patricia tree, with no indirect db abstraction. 

This project aims in removing the abstraction of the underlying storage and make Patricia tree work directly with data. This means, that all node types: branches, extensions (yet to be implemented) and leafs use internal addressing, requiring no additional database to get the data or write them.

Additionally, this means that Paprika uses **almost no managed memory**, delegating everything to the underlying store, based on memory mapped files. There are no managed nodes, no cache and no encoding/decoding. Just key-value pairs stored in a structure directly mapped to disk. 

The database assumes no deletes and keeping the last root in the memory. If deletes or pruning is required, it can be added later on. For example, by copying only values that are alive. The roots can be captured and stored elsewhere. They map to a single long and one can easily amend paprika to get old data by its root.

The current implementation **does not include**:

- Keccak compute nor storage
- RLP encoding

## Benchmarks

A few scenarios used for benchmarking

### 15 millions of key value pairs

15 millions of key value pairs, with keys being 32 bytes (Keccak) and values being 32 bytes.

- Writing of 15 millions of items took 00:00:25.1832635
- Reading of 15 millions of items took 00:00:23.9572717

#### Memory Profiling

Almost no managed memory used

![image](https://user-images.githubusercontent.com/519707/204166299-81c05582-7e0d-4401-b2cf-91a3c1b7153b.png)

#### Performance profiling

Some potential for extracting scope when writing items down

![image](https://user-images.githubusercontent.com/519707/204166363-afe54fec-d772-49ff-9d63-0bf7571b4294.png)

