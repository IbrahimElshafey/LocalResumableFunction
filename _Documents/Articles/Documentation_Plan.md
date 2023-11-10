# Write docs for

## How to use RF
* What are resumable functions?
	* Why resumable functions matter? 
	* Compare the resumable function and the tradtional method
	* What problems it solve?
* Wait Types
	* Method Wait
	* Group Wait
	* Function Wait
	* Replay/GoBack Waits
* UI
* Resumable functions logging
* RF Settings
* Distributed Services and Resumable Function/ Cross services waits
	* Orchestrator 
		* You will have one service that is an orchestrator and the other services will push calls to it.
		* You can have may instances of the orchestrator service.
	* Choreography
		* Services can comunicate with each other and wait calls from each other
* How to test your resumable functions?
* Some consideration when using the library
	* Serialization
	* Closures and private state

## How RF Works
* Intro about IAsyncEnumerable
* Function runner class
* How scanning works?
* Registering first wait
* How method push its arguments to the engine?
* How matched wait evaluation and processing works?
* Private variables in method 
	* Closure
	* Locals
* What is wait template?
* How concurrency is handled?
* Database cleaning

# Future Plan 
* Hou to publish new service version with modified resumable functions?
* Why not use service bus?
* Background processing and Hangfire
* 

* Write articles in
	* My github personal site
	* Code Project
	* Medium
	* c-sharpcorner.com