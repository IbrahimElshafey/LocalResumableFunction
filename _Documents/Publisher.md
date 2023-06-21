# Publisher Project
* Scan and send scan result to service owner to verify signatures
* Use PeriodicTimer to handle background tasks
	* Send failed requests to servies
	* Scan Dlls
* Use LiteDb to save:
	* Scan Data
	* Failed Requests
* Use .Net Standard