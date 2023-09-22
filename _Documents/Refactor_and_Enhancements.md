# Refactor
* Refactor long methods
* Optimize wait table indexes to enable fast wait insertion
	* Remove index 
		* [ParentWaitId] in [dbo].[Waits]
* Use IMaterializationInterceptor to set entity dependencies [Bad Practice as I think]

# Enhancements
* Message Pack private setter props serialization
* Parameter check lib use
* Use RequestedByFunctionId prop in TimeWaitInput to refine match for time waits


# Data store enhancements
* Store pushed calls in different store that support:
	* Fast insertion
	* Fast query by key (No other queries)
	* May be a queue service Kafka,RbbittMQ or ActiveMQ
	* May use pull mode to get pushed calls
* Store waits in different store that support:
	* Fast queries 
	* Fast wait insertion
	* May be an option https://github.com/VelocityDB/VelocityDB
* InMemory DBs
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
	* https://www.influxdata.com/

# New Features
* Target multiple runtimes
* Alternates is functions that trigger by the same pushed call but
	* Only one instance activated
	* Only one instance completed and other will be canceled
	* we may have multiple 
* Services registry is separate service
* Security and IUserContext
* Function priority/Matched Waits priority
	* How hangfire handle priority
* Performance Analysis/Monitoring
	* Pushed calls that is not necessary
	* How long processing a pushed call takes?
* Encryption option for sensitive data
	* Function state
	* Match and SetData Expressions
* Localization

# May be good to use
* How can I benefit from Azure Service Fabric
	* https://learn.microsoft.com/en-us/azure/service-fabric/service-fabric-overview


# Questions
* How to check that pushed calls processed by all affected services?

