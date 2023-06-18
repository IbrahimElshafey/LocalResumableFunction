
# Refactor
* Refactor long methods
* Remove direct use for DbContext
* What are best practices for HTTPClient use `services.AddSingleton<HttpClient>();`
* Replace HttpHangfire with abstraction to enable queue based communication


# Enhancements
* Function priority
	* How hangfire handle priority
* Parameter check lib use
* Performance Analysis
* Store options
	* Use Queue Service to Handle Pushed Calls
		* Kafka,RbbittMQ or ActiveMQ
	* Use Queue Service that support queries for fast wait insertion
* Use .net standard for Handler project
* Encryption option sensitive data
	* Function state
	* Match and SetData Expressions
* Resumable function hooks
	* After Resumed
	* On Error Occurred
* Use pull mode to get calls from a queue