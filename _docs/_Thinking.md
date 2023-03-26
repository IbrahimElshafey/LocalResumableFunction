# Interscervices Waits

* Every serive handle it's own waits
* If wait is not owned by current service it will notify the owner service to handle it



* Use background tasks instead of hangfire
	* Fire and forget push method called
	* Time wait 
	* Periodic check for unhandled matched waits
* https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio


* Service
	* Background Timer Service
	* Background Handle Pushes Service
		* Queue sevice
		* Periodic
		* Can be notified by external
		* Queue matched waits for same state

#Timer Service
* Using Quartz.NET with ASP.NET Core and worker services
https://andrewlock.net/using-quartz-net-with-asp-net-core-and-worker-services/