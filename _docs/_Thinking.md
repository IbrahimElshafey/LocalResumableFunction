# Interscervices Waits
* Share database between services
* If method wait called and some waits matched that not in current service
* Call the other service that own the wait
* This means there is no broker/mediator
* Use hangfire on each service to
	* Time wait 
	* Fire and forget method called
	* Call another service to pass matched wait message