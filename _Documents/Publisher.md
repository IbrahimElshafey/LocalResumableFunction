# Publisher Project
* Review CanPublishFromExternal and IsLocalOnly
	* Should it defined for method group or method idenetifier
* Pushed call may have flag fields for:
	* Is from external
	* From service
	* To Service
	* Processing behavior (Process Locally, or propagate in cluster)

* Save message to disk and delete it if success (will be option in settings named SaveBeforeSend)
* Retry policy for the failed requests before save to failed requests
* When to stop/block pushing call to the RF service

# RF Service and Client Coordination/Communication
* Must be from client to server and not reverse since client may be any type mobile,desktop, or web server.
	* Client ask to get service Id
	* Client register himself in a service
		* Security and how to verify
	* Client unregister himself from a service
	* Client verify that server register or define exetrnal calls (Methods exist on RF server)
	* Client verify in/out is same on server and client
	* Client ask for blocked calls list 
	* Client ask to remove block for a method
	* Generate classes for external calls and send it to server

# One-time guaranteed delivery
* Each client will have a unique Id
* The message Id will have an incermental ID
* The client will send the message and wait for the acknowledgment
* If the client failed to receive the acknowledgment it will store it to failed requests db on disk
* If the client sent a failed request the server will check the expectation sliding window