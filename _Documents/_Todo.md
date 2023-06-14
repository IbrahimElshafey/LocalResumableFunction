# Todo

## Core functions
* Props in expressions must be public and have getter and setter
* Write test cases for expressions rewrite
* Local variables when evaluating expression constants
* Delay processing wait if the scan is in progress
* Remove direct use for DbContext
* Same DLL in two services
* Review all places where database update occurs
* Function priority
	* How hangfire handle priority
* Can I use .net standard for Handler project
* Use pull mode to get pushed calls from a queue
* Replace HttpHangfire with abstraction to enable queue based communication

## Enhancements
* Refactor long methods
* Write Roslyn analyzer to force the right use for the library
* Parameter check lib use
* Performance Analysis
* Store options
	* Use Queue Service to Handle Pushed Calls
		* Kafka,RbbittMQ or ActiveMQ
* What are best practices for HTTPClient use `services.AddSingleton<HttpClient>();`

* Encryption option sensitive data
	* Function state
	* Match and SetData Expressions
* Resumable function hooks
	* After Resumed
	* On Error Occurred



# External Waits (Will be separate projects)
* Monitor network requests using reverse proxy and push MethodCalls [TCP Listener]
	* https://github.com/microsoft/reverse-proxy
* File/Folder Changes [File Watcher]
* RabbitMQ or any service bus [Subscribe to event]