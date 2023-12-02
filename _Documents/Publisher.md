# Publisher Project
* Review CanPublishFromExternal and IsLocalOnly
	* Should it defined for method group or method idenetifier
* Pushed call may have flag fields for:
	* Is from external
	* From service
	* To Service
	* Processing behavior (Process Locally, or propagate in cluster)
* Should we define an external id for pushed call?

* Retry policy for the failed requests before save to failed requests
* When to stop/block pushing call to the RF service
* RF Service and Client Coordination
	* Must be from client to server and not reverse since client may be any type mobile,desktop, or web server.
	* Client ask for blocked calls list 
	* Client ask to remove block for a method
	* Client verify that server register/define exetrnal calls (Methods exist on RF server)
	* Client verify in/out is same on server and client
	* Generate class for external calls and send it to server
###########################################################
* Each client will have a unique Id
* The message Id will be incermental number
* The client will send the message and wait for the acknowledgment
* If the client failed to receive the acknowledgment it will store it to failed requests db on disk
* If the client sent a failed request the server will d