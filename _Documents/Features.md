# Features
* Build Nuget package to target multiple runtimes and versions
* Security and IUserContext
* Function priority/Matched Waits priority
	* How hangfire handle priority

* Performance Analysis/Monitoring
	* Pushed calls that is not necessary
	* How long processing a pushed call takes?
* Encryption option for sensitive data
	* Function state
	* Match and SetData Expressions
* Services registry is separate service

# Questions
* How to check that pushed call processed by all affected services?
	* How to check that pushed call can be deleted?


* Alternates is functions that trigger by the same pushed call but
	* Only one instance activated
	* Only one instance completed and other will be canceled
	* [Canceled] we can implement this behaviour without need for custom handlinng
		* The first wait in alternates group will be waiting two wiats (the original wait and, the cancel wait)
		* When one of alternates matched it push cancel call to the engine
