# Data store enhancements
* Pushed Calls,Waits, Logging, and States
* We should not use SQL servers or any commercial DB
	* With current implementation we can use PostgreSQL, MySQL, MariaDB, Firebird but I didn't test them yet
* Store pushed calls in different store that support:
	* Fast insertion
	* Fast query by key (No other queries)
	* May I use Faster from Microsoft
* Store waits in different store that support:
	* Fast queries 
	* Fast wait insertion
	* InMemory DBs May be an option
		* May I use https://ignite.apache.org/docs/latest/ which is a distributed database for high-performance computing with in-memory speed.
		* https://hazelcast.com/clients/dotnet/
		* https://www.couchbase.com/
		* https://github.com/DevrexLabs/memstate
* Fast logging
	* Separate data store for log
	* Logs can be queried by
		* Date
		* Service Id
		* EntityName, EntityId
		* Status
	* Custom implementation for logging (IResumableFunctionLogging)
	* May I use InfluxDB,RocksDB or Faster