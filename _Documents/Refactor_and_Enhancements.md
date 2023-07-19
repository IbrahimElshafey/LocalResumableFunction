# Refactor
* Refactor long methods
* Optimize wait table indexes to enable fast wait insertion
	* Remove index 
		* [ParentWaitId] in [dbo].[Waits]
* Use IMaterializationInterceptor to set entity dependencies [Bad Practice as I think]

# Enhancements
* Use same dll in two different services must be allowed
* Message Pack private setter props serialization
* Parameter check lib use
* Resumable function hooks
	* After Resumed
	* On Error Occurred
* Use RequestedByFunctionId prop in TimeWaitInput to refine match for time waits
* ID must be long for
	* Pushed Call
	* Wait
	* WaitForCalls
	* Logs

# Data store enhancements
* Store pushed calls in different store that support:
	* Fast insertion
	* Fast query by key (No other query)
	* May be a queue service Kafka,RbbittMQ or ActiveMQ
	* May use pull mode to get pushed calls
* Store waits in different store that support:
	* Fast queries 
	* Fast wait insertion
	* May be an option https://github.com/VelocityDB/VelocityDB
* Fast logging
	* Separate data store for log
	* Logs can be queried
	* Custom implementation for logging (IResumableFunctionLogging)
	* https://www.influxdata.com/

# New Features
* Services Registry is separate
* Security and IUserContext
* Function priority/Matched Waits priority
	* How hangfire handle priority
* Performance Analysis/Monitoring
* Encryption option for sensitive data
	* Function state
	* Match and SetData Expressions
* Localization
* Replace HangfireHttpClient with abstraction to enable queue based communication between services

# May be good to use
* How can I benefit from Azure Service Fabric
	* https://learn.microsoft.com/en-us/azure/service-fabric/service-fabric-overview

* How to migrate to the new version
