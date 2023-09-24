# Data store enhancements
* We should not use SQL servers or any commercial DB
	* With current implementation we can use PostgreSQL, MySQL, MariaDB, Firebird
* Store pushed calls in different store that support:
	* Fast insertion
	* Fast query by key (No other queries)
	* May I use RocksDB
* Store waits in different store that support:
	* Fast queries 
	* Fast wait insertion
	* InMemory DBs May be an option
		* May I use https://ignite.apache.org/docs/latest/ which is a distributed database for high-performance computing with in-memory speed.
		* https://hazelcast.com/clients/dotnet/
		* https://www.couchbase.com/
* Fast logging
	* Separate data store for log
	* Logs can be queried by
		* Date
		* Service Id
		* EntityName, EntityId
		* Status
	* Custom implementation for logging (IResumableFunctionLogging)
	* May I use InfluxDB or RocksDB
===============
If you need a data store that supports fast insertion and fast query by key, particularly for scenarios where you don't need to update the data once it's stored, there are several options to consider, each with its strengths and trade-offs:

1. **Key-Value Stores:**
    - **Redis:** Redis is an in-memory key-value store known for its extremely fast data insertion and retrieval times. It's particularly suitable for caching and real-time applications.

2. **Column-Family Stores:**
    - **Apache Cassandra:** Cassandra is designed for high write and read throughput. It excels at horizontal scalability and can handle massive amounts of data with fast reads by key.

3. **Log-Structured Merge (LSM) Trees:**
    - **RocksDB:** RocksDB is an embedded key-value store that uses LSM trees. It's optimized for fast writes and supports efficient point lookups by key.

4. **Document Stores:**
    - **MongoDB:** While MongoDB is known for its flexible document model, it can also be used as a key-value store for fast insertions and key-based queries.

5. **Time-Series Databases:**
    - **InfluxDB:** InfluxDB is designed specifically for time-series data, making it an excellent choice for fast insertion and retrieval of time-stamped data points.

6. **NewSQL Databases:**
    - **VoltDB:** VoltDB is an in-memory NewSQL database designed for high-speed transactional processing. It can offer fast insertions and key-based queries.

7. **Memory-Mapped Databases:**
    - **Memcached:** Memcached is an in-memory key-value store that provides lightning-fast read and write operations, making it ideal for caching scenarios.

8. **Graph Databases:**
    - **RedisGraph:** If your data has graph-like structures, RedisGraph can provide fast insertion and querying capabilities for graph data.

9. **Custom Solutions:**
    - Depending on your specific requirements and constraints, you might consider building a custom solution using technologies like memory-mapped files or specialized data structures in programming languages like C/C++ or Rust.

When choosing the right data store, consider factors such as the volume of data, required scalability, latency tolerance, and any specific querying needs you have. Additionally, consider the ease of integration with your existing tech stack and the level of operational maintenance required for each option. Conducting benchmark tests with your actual data and usage patterns can help you determine which data store best meets your performance requirements.