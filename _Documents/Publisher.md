# Publisher Project
* Retry policy for the failed requests
* When to stop/block pushing call to the RF service
* RF Service and Client Coordination
	* Must be from client to server and not reverse since client may be any type mobile,desktop, or web server.
	* Client ask for blocked calls list
	* Client ask to remove block for a method
	* Client verify that server register/define exetrnal calls (Methods exist on RF server)
	* Client verify in/out is same on server and client
	* Generate class for external calls and send it to server