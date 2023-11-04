# Features
* Build Nuget package to target multiple runtimes and versions
* Security and IUserContext

* Services registry is separate service
* Function priority/Matched Waits priority
	* How hangfire handle priority
* Performance Analysis/Monitoring
	* Pushed calls that is not necessary
	* How long processing a pushed call takes?
* Encryption option for sensitive data
	* Function state
	* Match and SetData Expressions

# Questions
* How to check that pushed call processed by all affected services?
	* How to check that pushed call can be deleted?


* Alternates is functions that trigger by the same pushed call but
	* Only one instance activated
	* Only one instance completed and other will be canceled
	* we may have multiple 
