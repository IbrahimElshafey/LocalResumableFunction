# Features
* Build Nuget package to target multiple runtimes and versions
* Security and IUserContext
* Alternates is functions that trigger by the same pushed call but
	* Only one instance activated
	* Only one instance completed and other will be canceled
	* we may have multiple 
* Services registry is separate service
* Function priority/Matched Waits priority
	* How hangfire handle priority
* Performance Analysis/Monitoring
	* Pushed calls that is not necessary
	* How long processing a pushed call takes?
* Encryption option for sensitive data
	* Function state
	* Match and SetData Expressions


# May be good to use
* How can I benefit from Azure Service Fabric
	* https://learn.microsoft.com/en-us/azure/service-fabric/service-fabric-overview


# Questions
* How to check that pushed calls processed by all affected services?

