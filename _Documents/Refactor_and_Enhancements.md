
# Refactor
* Refactor long methods
* Optimize wait table indexes to enable fast wait insertion
	* Remove index 
		* [ParentWaitId] in [dbo].[Waits]
* Replace HangfireHttpClient with abstraction to enable queue based communication between services
* Use IMaterializationInterceptor to set entity dependencies

# Enhancements
* Services Registry is separate
* Pushed Call is separate
* Security and IUserContext
* Fast logging
	* Separate DB for log
	* Custom implementation for logging (IResumableFunctionLogging)
	* https://www.influxdata.com/
* Parameter check lib use
* Resumable function hooks
	* After Resumed
	* On Error Occurred
* Function priority/Matched Waits priority
	* How hangfire handle priority
* Performance Analysis/Monitoring
* Store options
	* Use Queue Service to Handle Pushed Calls
		* Kafka,RbbittMQ or ActiveMQ
	* Use Queue Service that support queries for fast wait insertion
* Encryption option for sensitive data
	* Function state
	* Match and SetData Expressions
* Use pull mode to get calls from a queue
* How can I benefit from Azure Service Fabric
	* https://learn.microsoft.com/en-us/azure/service-fabric/service-fabric-overview
* ID must be long for
	* Pushed Call
	* Wait
	* WaitForCalls
	* Logs
* Localization