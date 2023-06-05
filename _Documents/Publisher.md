# Publisher Project
* [Test] AspectInjector instead of Fody 
* Scan and send scan result to service owner to verify signatures
* Use PeriodicTimer/Hangfire to handle background tasks
	* Send failed requests to servies
	* Scan Dlls
* DB
	* Use LiteDb to save scan Data and requests
	* Or https://github.com/hhblaze/DBreeze