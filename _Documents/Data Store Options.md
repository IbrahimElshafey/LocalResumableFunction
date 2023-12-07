# Data Store Enhancements for (Pushed Calls,Waits, Logging, and States)
* We should not use SQL servers or any commercial DB
	* With current implementation we can use PostgreSQL, MySQL, MariaDB, Firebird but I didn't test them yet
* I need database that
	* Support EF
	* Keeps hot data in memory
	* Saves cold data to disk
	* 
## Pushed Calls
* Used 
	* When method executed and call pushed
	* When clean database to delete old pushed calls
	* 
* Store pushed calls in different store that support:
	* Fast insertion
	* Fast query by key (No other queries)
	* May I use:
		* Faster from Microsoft
		* A fast store and forward message queue for .NET. (aka not a broker or server)
			* https://github.com/LightningQueues/LightningQueues
			* https://github.com/zeromq/netmq
## Waits
* Store waits in different store that support:
	* Fast queries
	* Fast wait insertion
	* InMemory DBs May be an option
		* May I use https://ignite.apache.org/docs/latest/ which is a distributed database for high-performance computing with in-memory speed.
		* https://hazelcast.com/clients/dotnet/
		* https://www.couchbase.com/
		* https://github.com/DevrexLabs/memstate
## Logs
* Fast logging
	* Separate data store for log
	* Logs can be queried by
		* Date
		* Service Id
		* EntityName, EntityId
		* Status
	* Custom implementation for logging (IResumableFunctionLogging)
	* May I use InfluxDB,RocksDB or Faster
## States and private data
	* 


# We need In-Memory DB that:
* Can be used with EF
* Can be used with Hangfire
* Support Snapshoting and clustering
* Mark some tables as on disk only
* Keep rows in memory based on condition
* Keep rows in memory based on root node is live
* Execute quries againist memory nad don't hit db
