
# Refactor
* Refactor long methods
* Optimize wait table indexes to enable fast wait insertion
	* Remove index 
		* [ParentWaitId] in [dbo].[Waits]

# Enhancements
* Function priority/Matched Waits priority
	* How hangfire handle priority
* Parameter check lib use
* Performance Analysis
* Store options
	* Use Queue Service to Handle Pushed Calls
		* Kafka,RbbittMQ or ActiveMQ
	* Use Queue Service that support queries for fast wait insertion
* Encryption option for sensitive data
	* Function state
	* Match and SetData Expressions
* Resumable function hooks
	* After Resumed
	* On Error Occurred
* Use pull mode to get calls from a queue
* How can I benefit from Azure Service Fabric
	* https://learn.microsoft.com/en-us/azure/service-fabric/service-fabric-overview
